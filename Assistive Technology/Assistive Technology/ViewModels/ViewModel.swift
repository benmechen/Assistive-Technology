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
    func disconnected(_ message: [String: String]?)
    func set25Bar(_ on: Bool)
    func set50Bar(_ on: Bool)
    func set75Bar(_ on: Bool)
    func set100Bar(_ on: Bool)
}

extension ViewModelDelegate {
    func disconnected(_ message: [String: String]? = nil) {
        disconnected(message)
    }
}

class ViewModel: ConnectionServiceDelegate {
    
    var delegate: ViewModelDelegate?
    var connectionService: ConnectionService?
    var connectionState: ConnectionService.State = .disconnected
    var loadingTimer: Timer?
    var loadingBar = 0
    
    func connectionState(state: ConnectionService.State) {
        guard state != self.connectionState else {
            // Don't update if already know status
            return
        }
        
        self.connectionState = state
        
        switch state {
        case .connected:
            DispatchQueue.main.async {
                self.delegate?.connected()
            }
        case .disconnected:
            DispatchQueue.main.async {
                self.delegate?.disconnected()
            }
        case .failed(let error):
            guard error != .connectAlreadyConnected else {
                // Already connected, not a problem so don't tell user
                return
            }
            
            let title = "An error occured connecting to the controlling device"
            var message = ""
            
            switch error {
            case .connectAddressUnavailable:
                message = "Unable to connect to the server with supplied address. Try restarting the moble and server applications."
            case .connectCanceled:
                message = "The connection request was canceled"
            case .connectDeviceBusy:
                message = "The server refused the connection. Try restarting the server application."
            case .connectHostDown:
                message = "Unable to connect to the server. Make sure the server application is running."
            case .connectNetworkDown:
                message = "The network is down. Make sure both your mobile device and server device are connected to the same WiFi network."
            case .connectPermissionDenied:
                message = "Permission denied by the mobile device. Please try again, or restart the mobile app if the issue continues."
            case .connectRefused:
                message = "The connection was refused by the server. Make sure no other devices are connected already"
            case .connectTimeout:
                message = "The connection timed out. Please try again later, or reconnect to the sever if the issue continues."
            default:
                message = "Please try again. If the issue persists, restart both your mobile device and the server device."
            }
            
            DispatchQueue.main.async {
                self.delegate?.disconnected(["title": title, "message": message])
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
        
        if self.connectionState == .connected {
            stop()
        } else {
            start()
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
