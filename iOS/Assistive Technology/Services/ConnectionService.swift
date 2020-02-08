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

protocol ConnectionServiceDelegate {
    func connectionState(state: ConnectionService.State)
    func connectionStrength(strength: Float)
}

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
    
    enum State: Equatable {
        case connected
        case connecting
        case disconnected
        case failed(ConnectionServiceError)
    }
    
    deinit {
        killClocks()
    }
    
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
    
    private func killClocks() {
        for i in 0...self.previousClocks.count {
            // Concurrency fix
            guard i < self.previousClocks.count else { return }
            self.previousClocks[i].invalidate()
            self.previousClocks.remove(at: i)
        }
    }
    
    private func set(state: ConnectionService.State) {
        self.state = state
        self.delegate?.connectionState(state: state)
    }
    
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

extension ConnectionService: NetServiceBrowserDelegate, NetServiceBrowserDelegateExtension, NetServiceDelegate {
    var discovered: Bool {
        get {
            return self._discovered
        }
        set {
            self._discovered = newValue
        }
    }
    
    public func discover() {
        self.set(state: .connecting)
        service = nil
        _discovered = false
        browser.delegate = self
        browser.stop()
        browser.searchForServices(ofType: "_assistive-tech._udp", inDomain: "", withTimeout: 5.0)
    }
    
    // MARK: Service Discovery
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

    func netServiceBrowserDidStopSearch(_ browser: NetServiceBrowser, success: Bool) {
        if !success {
            self.set(state: .failed(.discoverTimeout))
        }
    }
    
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
    
    func netServiceDidResolveAddress(_ sender: NetService) {
        if let serviceIp = resolveIPv4(addresses: sender.addresses!) {
            self.connect(to: serviceIp, on: 1024)
        } else {
            self.set(state: .failed(.discoverResolveFailed))
        }
    }
    
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
