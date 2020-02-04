//
//  ConnectionService.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 03/02/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation
import CocoaAsyncSocket

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
    func connectionUpdate(connected: Bool)
}

class ConnectionService: NSObject, GCDAsyncUdpSocketDelegate {
    static let shared = ConnectionService()
    var delegate: ConnectionServiceDelegate?
    private var subscribers: [ConnectionServiceDelegate] = []
    private var socket: GCDAsyncUdpSocket?
    private var browser = NetServiceBrowser()
    private var service: NetService?
    private var queue = DispatchQueue(label: "ConnectionServiceQueue")
    private var server: Server?
    private var sent: Float = 0.0
    private var received: Float = 0.0
    private var open: Bool = false
    
    struct Server {
        var host: NSString = NSString(string: "255.255.255.255")
        var port: UInt16
    }

    override init() {
        super.init()
        
        socket = GCDAsyncUdpSocket(delegate: self, delegateQueue: queue)
        socket?.setIPv4Enabled(true)
        socket?.setIPv6Enabled(false)
    }
    
    public func connect(to host: String, on port: UInt16) {
        self.server = Server(host: NSString(string: host), port: port)
        
        do {
            try socket?.bind(toPort: port)
            try socket?.enableBroadcast(true)
            try socket?.beginReceiving()
            try socket?.connect(toHost: host, onPort: port)
            
            print(" > Connection started on \(socket?.connectedHost() ?? "-")")
            self.send(AssistiveTechnologyProtocol.discover.rawValue)
        } catch let error as NSError {
            print(error)
        }
    }
    
    public func send(_ value: String) {
//        guard self.open else { return }
        guard let data = value.data(using: .utf8), let addressData = server?.host.data(using: String.Encoding.utf8.rawValue) else { return }
        
        self.socket?.send(data, toAddress: addressData, withTimeout: 2, tag: 0)
        print(" > Sent: \(data as NSData) string: \(value) to: \(server?.host)")

        self.sent += 1
    }
    
    public func close() {
        self.send(AssistiveTechnologyProtocol.disconnect.rawValue)
        delegate?.connectionUpdate(connected: false)
        self.socket?.close()
    }
    
    func udpSocket(_ sock: GCDAsyncUdpSocket, didReceive data: Data, fromAddress address: Data, withFilterContext filterContext: Any?) {
        
        if let message = NSString(data: data, encoding: String.Encoding.utf8.rawValue) as String? {
            if message.contains(AssistiveTechnologyProtocol.acknowledge.rawValue) {
                self.received += 1
            }
            
            if message.contains(AssistiveTechnologyProtocol.handshake.rawValue) {
                self.open = true
                delegate?.connectionUpdate(connected: true)
            }
            
            let percent: Float = (self.received / self.sent) * 100
            
            print(" > Received: \(data as NSData) string: \(message) -- \(percent)% successfull transmission")
        }
    }
}

extension ConnectionService: NetServiceBrowserDelegate, NetServiceDelegate {
    public func discover() {
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
      print("Resolve error:", sender, errorDict)
    }

    func netServiceBrowserDidStopSearch(_ browser: NetServiceBrowser) {
      print("Search stopped")
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
