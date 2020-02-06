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
    private var connection: NWConnection?
    private var browser = NetServiceBrowser()
    private var service: NetService?
    private var queue = DispatchQueue(label: "ConnectionServiceQueue")
    private var sent: Float = 0.0
    private var received: Float = 0.0
    private var strengthBuffer: [Float] = []
    private var lastSentClock: Timer?
    private var previousClocks: [Timer] = []
    private var open: Bool = false
    
    enum State: Equatable {
        case connected
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
                print("State: Ready\n")
                guard let connection = self.connection else {
                    return
                }
                
                self.open = true
                self.listen(on: connection)
                self.send(AssistiveTechnologyProtocol.discover.rawValue)
            case .failed(let error):
                switch error {
                case .posix(let code):
                    switch code {
                    case .EADDRINUSE, .EADDRNOTAVAIL:
                        self.delegate?.connectionState(state: .failed(.connectAddressUnavailable))
                    case .EACCES, .EPERM:
                        self.delegate?.connectionState(state: .failed(.connectPermissionDenied))
                    case .EBUSY:
                        self.delegate?.connectionState(state: .failed(.connectDeviceBusy))
                    case .ECANCELED:
                        self.delegate?.connectionState(state: .failed(.connectCanceled))
                    case .ECONNREFUSED:
                        self.delegate?.connectionState(state: .failed(.connectRefused))
                    case .EHOSTDOWN, .EHOSTUNREACH:
                        self.delegate?.connectionState(state: .failed(.connectHostDown))
                    case .EISCONN:
                        self.delegate?.connectionState(state: .failed(.connectAlreadyConnected))
                    case .ENOTCONN:
                        self.delegate?.connectionState(state: .disconnected)
                    case .ETIMEDOUT:
                        self.delegate?.connectionState(state: .failed(.connectTimeout))
                    case .ENETDOWN, .ENETUNREACH, .ENETRESET:
                        self.delegate?.connectionState(state: .failed(.connectNetworkDown))
                    default:
                        os_log(.error, "POSIX connection error: %@", code.rawValue)
                        self.delegate?.connectionState(state: .failed(.connectOther))
                    }
                default:
                    self.delegate?.connectionState(state: .failed(.connectOther))
                }
            default:
                print("ERROR! State not defined!\n")
            }
        }
        
        connection?.start(queue: queue)
        
        print(" > Connection started on \(self.connection?.endpoint.debugDescription ?? "-")")
    }
    
    public func send(_ value: String) {
        guard self.open else { return }
        guard let data = value.data(using: .utf8) else { return }
        
        self.connection?.send(content: data, completion: .contentProcessed( { error in
            if let error = error {
                print(error)
                return
            }
            
            if let previousClock = self.lastSentClock {
                self.previousClocks.append(previousClock)
            }
            
            DispatchQueue.main.async {
                self.lastSentClock = Timer.scheduledTimer(withTimeInterval: 2.0, repeats: false) { timer in
                    // No response received after 5 seconds, update connection status
                    if self.calculateStrength(rate: 0.0) < 5 {
                        self.close(false)
                    }
                }
            }
            
            self.sent += 1
            print(" > Sent: \(data as NSData) string: \(value)")
        }))
    }
    
    public func close(_ killServer: Bool = true) {
        if killServer {
            self.send(AssistiveTechnologyProtocol.disconnect.rawValue)
        }
        self.killClocks()
        delegate?.connectionState(state: .disconnected)
        self.connection?.cancel()
    }
    
    private func listen(on connection: NWConnection) {
        guard self.open else { return }
        
        connection.receiveMessage { (data, context, isComplete, error) in
            if (isComplete) {
                if let data = data, let message = String(data: data, encoding: .utf8) {
                    self.received += 1
                    
                    self.killClocks()
                    
                    if message.contains(AssistiveTechnologyProtocol.handshake.rawValue) {
                        self.open = true
                        self.delegate?.connectionState(state: .connected)
                    }
                    
                    if message.contains(AssistiveTechnologyProtocol.disconnect.rawValue) {
                        self.close()
                    }
                    
                    let percent: Float = (self.received / self.sent) * 100
                    
                    print(" > Received: \(data as NSData) string: \(message) -- \(self.calculateStrength(rate: percent))% successfull transmission")
                }
                
                if let error = error {
                    print(error)
                } else {
                    self.listen(on: connection)
                }
            }
        }
    }
    
    private func calculateStrength(rate percent: Float) -> Float {
        self.strengthBuffer.append(percent)
        
        self.strengthBuffer = Array(self.strengthBuffer.suffix(5))
        
        let average = self.strengthBuffer.average ?? 100.0
        self.delegate?.connectionStrength(strength: average)
        print(average)
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
}

extension ConnectionService: NetServiceBrowserDelegate, NetServiceDelegate {
    public func discover() {
        delegate?.connectionStrength(strength: -1)
        service = nil
        browser.delegate = self
        browser.stop()
        browser.searchForServices(ofType: "_assistive-tech._udp", inDomain: "")
    }
    
    // MARK: Service Discovery
    func netServiceBrowserWillSearch(_ browser: NetServiceBrowser) {
        print("Search about to begin")
    }
    
    func netService(_ sender: NetService, didNotResolve errorDict: [String : NSNumber]) {
        self.delegate?.connectionStrength(strength: 0)
        print("Resolve error:", sender, errorDict)
    }

    func netServiceBrowserDidStopSearch(_ browser: NetServiceBrowser) {
      print("Search stopped")
        self.delegate?.connectionStrength(strength: 0)
    }
    
    func netServiceBrowser(_ browser: NetServiceBrowser, didFind service: NetService, moreComing: Bool) {
        guard self.service == nil else {
            return
        }
        
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
        print("Resolved service")
      
        if let serviceIp = resolveIPv4(addresses: sender.addresses!) {
            self.connect(to: serviceIp, on: 1024)
            print("Found IPV4:", serviceIp)
        } else {
            print("Did not find IPV4 address")
        }
    }
    
    private func resolveIPv4(addresses: [Data]) -> String? {
      var result: String?

      for addr in addresses {
        let data = addr as NSData
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
