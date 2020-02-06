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
    func set25Bar(_ on: Bool)
    func set50Bar(_ on: Bool)
    func set75Bar(_ on: Bool)
    func set100Bar(_ on: Bool)
}

class ViewModel: ConnectionServiceDelegate {
    
    var delegate: ViewModelDelegate?
    var connectionService: ConnectionService?
    var isConnected: Bool = false
    var loadingTimer: Timer?
    var loadingBar = 0
    
    func connectionStatus(status: Bool) {
        guard status != self.isConnected else {
            // Don't update if already know status
            return
        }
        
        self.isConnected = status
        
        if status {
            DispatchQueue.main.async {
                self.delegate?.connected()
            }
        } else {
            DispatchQueue.main.async {
                self.delegate?.disconnected()
            }
        }
    }
    
    func connectionStrength(strength: Float) {
        if strength == -1 {
            DispatchQueue.main.async {
                self.loadingTimer = Timer.scheduledTimer(withTimeInterval: 0.25, repeats: true) { (timer) in
                    var bar25 = false
                    var bar50 = false
                    var bar75 = false
                    var bar100 = false
                    
                    if self.loadingBar == 0 {
                        bar25 = true
                        self.loadingBar += 1
                    } else if self.loadingBar == 1 {
                        bar50 = true
                        self.loadingBar += 1
                    } else if self.loadingBar == 2 {
                        bar75 = true
                        self.loadingBar += 1
                    } else if self.loadingBar == 3 {
                        bar100 = true
                        self.loadingBar = 0
                    }
                    
                    self.delegate?.set25Bar(bar25)
                    self.delegate?.set50Bar(bar50)
                    self.delegate?.set75Bar(bar75)
                    self.delegate?.set100Bar(bar100)
                }
            }
            
            return
        }
        
        self.loadingTimer?.invalidate()
        
        var bar25 = false
        var bar50 = false
        var bar75 = false
        var bar100 = false
        
        if strength > 0 {
            bar25 = true
        }
        
        if strength > 25 {
            bar50 = true
        }
        
        if strength > 50 {
            bar75 = true
        }
        
        if strength > 75 {
            bar100 = true
        }
        
        DispatchQueue.main.async {
            self.delegate?.set25Bar(bar25)
            self.delegate?.set50Bar(bar50)
            self.delegate?.set75Bar(bar75)
            self.delegate?.set100Bar(bar100)
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
        DispatchQueue.main.async {
            self.delegate?.set25Bar(false)
            self.delegate?.set50Bar(false)
            self.delegate?.set75Bar(false)
            self.delegate?.set100Bar(false)
        }

        connectionService?.close()
    }
}
