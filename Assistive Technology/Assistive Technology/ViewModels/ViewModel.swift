//
//  ViewModel.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 29/01/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation

protocol ViewModelDelegate {
    func connected()
    func disconnected()
}

class ViewModel: ConnectionServiceDelegate {
    
    var delegate: ViewModelDelegate?
    var connectionService: ConnectionService?
    var isConnected: Bool = false
    
    func connectionUpdate(connected: Bool) {
        self.isConnected = connected
        
        if connected {
            delegate?.connected()
        } else {
            delegate?.disconnected()
        }
    }
    
    public func toggleConnection() {
        if connectionService == nil {
            connectionService = ConnectionService()
            connectionService?.delegate = self
        }
        
        if !isConnected {
            start()
        } else {
            stop()
        }
    }
    
    public func sendDirection(_ message: AssistiveTechnologyProtocol) {
        connectionService?.send(message.rawValue)
    }
    
    private func start() {
        connectionService?.discover()
    }
    
    private func stop() {
        connectionService?.close()
    }
}
