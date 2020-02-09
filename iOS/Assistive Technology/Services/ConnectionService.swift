//
//  ConnectionService.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 03/02/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation
import Network
import os.log

/// Connection protocol with set messages to interact with the corresponding server
enum AssistiveTechnologyProtocol: String {
    case up = "astv_up"
    case down = "astv_down"
    case left = "astv_left"
    case right = "astv_right"
    case disconnect = "astv_disconnect"
    case discover = "astv_discover"
    case handshake = "astv_shake"
    case acknowledge = "astv_ack"
    case greet = "astv_greet"
}

/// Protocol for ConnectionService caller to conform to in order to received updates about the connection state and strength
protocol ConnectionServiceDelegate {
    func connectionState(state: ConnectionService.State)
    func connectionStrength(strength: Float)
}

/**
 Automatically discover, connect and communicate with a server comforming to the Assistive Technology protocol
 
 Caller must conform to the ConnectionServiceDelegate protocol to receive status updates
 */
class ConnectionService: NSObject {
    var delegate: ConnectionServiceDelegate?
    var state: ConnectionService.State = .disconnected
    private var connection: NWConnection?
    private var browser = NetServiceBrowser()
    private var service: NetService?
    private var queue = DispatchQueue(label: "ConnectionServiceQueue")
    private var sent: Float = 0.0
    private var received: Float = 0.0
    private var strengthBuffer: [Float] = []
    private var lastSentClock: Timer?
    private var previousClocks: [Timer] = []
    private var discoverTimeout: Int = 0
    private var _discovered = false
    
    /// The state of the connection handled by the service instance
    enum State: Equatable {
        case connected
        case connecting
        case disconnected
        case failed(ConnectionServiceError)
    }
    
    
    /// Remove any timeout clocks to save memory and avoid trying to close a dead connection
    deinit {
        killClocks()
    }
    
    /// Open a Network connection and greet the server
    ///
    /// Errors passed to delegate
    /// - Parameters:
    ///   - host: IP address to connect to
    ///   - port: Port on which to bind connection
    public func connect(to host: String, on port: UInt16) {
        let host = NWEndpoint.Host(host)
        guard let port = NWEndpoint.Port(rawValue: port) else {
            return
        }
        
        self.strengthBuffer.removeAll()
        
        self.connection = NWConnection(host: host, port: port, using: .udp)
        
        self.connection?.stateUpdateHandler = { (newState) in
            switch (newState) {
            case .ready:
                guard let connection = self.connection else {
                    return
                }

                self.listen(on: connection)
                self.discoverTimeout = 0
                self.send(AssistiveTechnologyProtocol.discover.rawValue)
            case .failed(let error), .waiting(let error):
                self.handle(NWError: error)
            default:
                break
            }
        }
        
        connection?.start(queue: queue)
        
        print(" > Connection started on \(self.connection?.endpoint.debugDescription ?? "-")")
    }
    
    /// Send a message to the server on the open connection
    ///
    /// When sending a message, wait 2 seconds before either trying to discover again or close the connection when the connection strength is less than threshold
    /// Errors passed to delegate
    /// - Warning: Will only send data if the connection is in the connected or connecting state
    /// - Parameter value: String to send to the server
    public func send(_ value: String) {
        guard self.state == .connected || self.state == .connecting else { return }
        guard let data = value.data(using: .utf8) else { return }
        
        self.connection?.send(content: data, completion: .contentProcessed( { error in
            if let error = error {
                self.handle(NWError: error)
                return
            }
            
            if self.state == .connected || self.state == .connecting {
                if let previousClock = self.lastSentClock {
                    self.previousClocks.append(previousClock)
                }
                
                DispatchQueue.main.async {
                    self.lastSentClock = Timer.scheduledTimer(withTimeInterval: 2.0, repeats: false) { timer in
                        // No response received after 5 seconds, update connection status
                        if self.calculateStrength(rate: 0.0) < 5 {
                            if self.state == .connecting {
                                guard self.discoverTimeout < 5 else {
                                    self.close(false, state: .failed(.connectShakeNoResponse))
                                    return
                                }
                                
                                self.send(AssistiveTechnologyProtocol.discover.rawValue)
                                self.discoverTimeout += 1
                            } else {
                                self.close(false)
                            }
                        }
                    }
                }
            }
            
            self.sent += 1
            print(" > Sent: \(data as NSData) string: \(value)")
        }))
    }
    
    /// Close the connection
    ///
    /// Removes all timers waiting for server response
    /// - Warning: Will only close the connection if in the connected or connecting states
    /// - Parameters:
    ///   - killServer: Send shutdown command to the server to stop the application (default true)
    ///   - state: State to set the connection to once killed (default disconnected state)
    public func close(_ killServer: Bool = true, state: ConnectionService.State = .disconnected) {
        guard self.state == .connected || self.state == .connecting else {
            // Connection closed already
            return
        }
        
        if killServer {
            self.send(AssistiveTechnologyProtocol.disconnect.rawValue)
        }
        self.killClocks()
        self.set(state: state)
        self.connection?.cancel()
    }
    
    /// Listen on open connection for incomming messages
    ///
    /// Interpret incomming messages according to AssistiveTechnologyProtocol
    /// Remove timeout
    /// Update strength
    /// Errors passed to delegate
    /// - Parameter connection: Open NWConnection to listen on
    private func listen(on connection: NWConnection) {
        connection.receiveMessage { (data, context, isComplete, error) in
            if (isComplete) {
                if let error = error {
                    self.handle(NWError: error)
                    return
                }
                
                if let data = data, let message = String(data: data, encoding: .utf8) {
                    self.received += 1
                    
                    self.killClocks()
                    
                    if message.contains(AssistiveTechnologyProtocol.handshake.rawValue) {
                        self.set(state: .connected)
                    }
                    
                    if message.contains(AssistiveTechnologyProtocol.disconnect.rawValue) {
                        self.close()
                    }
                    
                    let percent: Float = (self.received / self.sent) * 100
                    
                    print(" > Received: \(data as NSData) string: \(message) -- \(self.calculateStrength(rate: percent))% successfull transmission")
                }

                self.listen(on: connection)
            }
        }
    }
    
    /// Calculate success rate of sent packets based on acknowledgement packets received from server
    ///
    /// Average of the last 5 strength values
    /// Update the delegate with the connection strength
    /// - Parameter percent: Current success percentage calculated from the number of sent and received packets
    private func calculateStrength(rate percent: Float) -> Float {
        guard self.state == .connected else {
            self.delegate?.connectionStrength(strength: 0)
            return 0
        }
        
        self.strengthBuffer.append(percent)
        
        self.strengthBuffer = Array(self.strengthBuffer.suffix(5))
        
        let average = self.strengthBuffer.average ?? 100.0
        self.delegate?.connectionStrength(strength: average)
        return average
    }
    
    /// Remove all timeout clocks currently awaiting a response
    private func killClocks() {
        for i in 0...self.previousClocks.count {
            // Concurrency fix
            guard i < self.previousClocks.count else { return }
            self.previousClocks[i].invalidate()
            self.previousClocks.remove(at: i)
        }
    }
    
    /// Update current state and inform delegate
    /// - Parameter state: New state
    private func set(state: ConnectionService.State) {
        self.state = state
        self.delegate?.connectionState(state: state)
    }
    
    /// Handle errors in the NWError format and set the service state
    /// - Parameter error: Error received from NWConnection
    private func handle(NWError error: NWError) {
        switch error {
        case .posix(let code):
            switch code {
            case .EADDRINUSE, .EADDRNOTAVAIL:
                self.state = .failed(.connectAddressUnavailable)
                self.set(state: .failed(.connectAddressUnavailable))
            case .EACCES, .EPERM:
                self.set(state: .failed(.connectPermissionDenied))
            case .EBUSY:
                self.set(state: .failed(.connectDeviceBusy))
            case .ECANCELED:
                self.set(state: .failed(.connectCanceled))
            case .ECONNREFUSED:
                self.set(state: .failed(.connectRefused))
            case .EHOSTDOWN, .EHOSTUNREACH:
                self.set(state: .failed(.connectHostDown))
            case .EISCONN:
                self.set(state: .failed(.connectAlreadyConnected))
            case .ENOTCONN:
                self.set(state: .disconnected)
            case .ETIMEDOUT:
                self.set(state: .failed(.connectTimeout))
            case .ENETDOWN, .ENETUNREACH, .ENETRESET:
                self.set(state: .failed(.connectNetworkDown))
            default:
                os_log(.error, "POSIX connection error: %@", code.rawValue)
                self.set(state: .failed(.connectOther))
            }
        default:
            self.set(state: .failed(.connectOther))
        }
    }
}

// MARK: NetService extension
extension ConnectionService: NetServiceBrowserDelegate, NetServiceBrowserDelegateExtension, NetServiceDelegate {
    var discovered: Bool {
        get {
            return self._discovered
        }
        set {
            self._discovered = newValue
        }
    }
    
    /// Begin looking for the server advertising with the Bonjour protocol
    ///
    /// Set state to connecting
    /// Start browsing for services
    /// - Parameter type: Type of service to discover
    public func discover(type: String) {
        self.set(state: .connecting)
        service = nil
        _discovered = false
        browser.delegate = self
        browser.stop()
        browser.searchForServices(ofType: type, inDomain: "", withTimeout: 5.0)
    }
    
    // MARK: Service Discovery
    /// Browser stopped searching for service
    ///
    /// Modified to add success parameter to set state to failed if the search timed out
    /// - Parameters:
    ///   - browser: Browser instance
    ///   - success: Did the browser discover the service in time
    func netServiceBrowserDidStopSearch(_ browser: NetServiceBrowser, success: Bool) {
        if !success {
            self.set(state: .failed(.discoverTimeout))
        }
    }
    
    /// Browser found a matching service
    ///
    /// Set discovered parameter for NetServiceBrowser for success parameter in `netServiceBrowserDidStopSearch()`
    /// Resolve server's IP
    /// - Parameters:
    ///   - browser: Browser instance
    ///   - service: Service found
    ///   - moreComing: Were more services discovered
    func netServiceBrowser(_ browser: NetServiceBrowser, didFind service: NetService, moreComing: Bool) {
        self._discovered = true
        
        guard self.service == nil else {
            return
        }
        
        self.discovered = true
        
        self.set(state: .connecting)
        
        print("Discovered the service")
        print("- name:", service.name)
        print("- type", service.type)
        print("- domain:", service.domain)

        browser.stop()
        
        self.service = service
        self.service?.delegate = self
        self.service?.resolve(withTimeout: 5)
    }
    
    // MARK: Resolve IP Service
    /// Handle NetService errors, set connection state according to given error
    /// - Parameters:
    ///   - sender: Resolve service
    ///   - errorDict: Errors from NetService
    func netService(_ sender: NetService, didNotResolve errorDict: [String : NSNumber]) {
        for key in errorDict.keys {
            switch errorDict[key] {
            case -72002:
                self.set(state: .failed(.discoverResolveServiceNotFound))
            case -72003:
                self.set(state: .failed(.discoverResolveBusy))
            case -72004, -72006:
                self.set(state: .failed(.discoverIncorrectConfiguration))
            case -72005:
                self.set(state: .failed(.discoverResolveCanceled))
            case -72007:
                self.set(state: .failed(.discoverResolveTimeout))
            default:
                self.set(state: .failed(.discoverResolveUnknown))
            }
        }
    }
    
    /// Resolve service got an IP address of the discovered server and connect to the server at that address
    /// - Parameter sender: Resolve service
    func netServiceDidResolveAddress(_ sender: NetService) {
        if let serviceIp = resolveIPv4(addresses: sender.addresses!) {
            self.connect(to: serviceIp, on: 1024)
        } else {
            self.set(state: .failed(.discoverResolveFailed))
        }
    }
    
    /// Get server IP address from list of address data
    /// - Parameter addresses: List of address data
    /// - Returns: Server IP address if found
    private func resolveIPv4(addresses: [Data]) -> String? {
        var result: String?

        for address in addresses {
            let data = address as NSData
            var storage = sockaddr_storage()
            data.getBytes(&storage, length: MemoryLayout<sockaddr_storage>.size)

            if Int32(storage.ss_family) == AF_INET {
                let addr4 = withUnsafePointer(to: &storage) {
                    $0.withMemoryRebound(to: sockaddr_in.self, capacity: 1) {
                        $0.pointee
                    }
                }

                if let ip = String(cString: inet_ntoa(addr4.sin_addr), encoding: .ascii) {
                    result = ip
                    break
                }
            }
        }

        return result
    }
}
