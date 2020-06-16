//
//  ViewModel.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 29/01/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation

/// Protocol for views to subscribe to in order to receive updates
protocol ConnectViewModelDelegate {
    /// Connection is in progress
    func connecting()
    /// Connection successfully opened
    func connected()
    /// Connection disconnected, either due to the user closing the connection or an error
    /// - Parameter message: Error message to be displayed to user
    func disconnected(_ message: [String: String]?)
    /// Set the 25% bar state
    /// - Parameter on: Bar full or empty
    func set25Bar(_ on: Bool)
    /// Set the 50% bar state
    /// - Parameter on: Bar full or empty
    func set50Bar(_ on: Bool)
    /// Set the 75% bar state
    /// - Parameter on: Bar full or empty
    func set75Bar(_ on: Bool)
    /// Set the 100% bar state
    /// - Parameter on: Bar full or empty
    func set100Bar(_ on: Bool)
}

extension ConnectViewModelDelegate {
    /// Connection disconnected,  either due to the user closing the connection or an error.
    /// - Note: Extension allows no message to be set
    /// - Parameter message: Optional error message to be displayed to user
    func disconnected(_ message: [String: String]? = nil) {
        disconnected(message)
    }
}

/// Coordinates between the UIViewController and Models/Services for the Connect View
/// This class is the designated ConnectionService delegate in order to receive updates. It is done this way due to ConnectionService being refactored into a singleton class.
/// - Note: Implement the ViewModelDelegate protocol in order to subscribe to updates
class ConnectViewModel: ConnectionServiceDelegate {
    /// View subscribing to ViewModel, used to update UI
    var delegate: ConnectViewModelDelegate?
    /// Timer responsible for animating the loading bars
    var loadingTimer: Timer?
    /// Currently set bar, for connection loading animation. Max 3, before going back to 0
    var loadingBar = 0
    
    init() {
        ConnectionService.shared.delegate = self
        ConnectionService.shared.fetchConnectionStrength()
    }
    
    public func getConnectionState() -> ConnectionService.State {
        return ConnectionService.shared.state
    }
    
    // MARK: Protocol implementation
    /// Updates the current state of the connection
    /// Updates view according to new state
    /// Handles errors and generates user facing messages for view to display
    /// Tells any subscribers that the network state has changed
    /// - Note: `ConnectionServiceDelegate` function implementation
    /// - Parameter state: New state
    func connectionState(state: ConnectionService.State) {    
        switch ConnectionService.shared.state {
        case .connected:
            DispatchQueue.main.async {
                self.delegate?.connected()
                NotificationCenter.default.post(name: .connected, object: nil)
            }
        case .disconnected:
            DispatchQueue.main.async {
                self.connectionStrength(strength: 0)
                self.delegate?.disconnected()
                NotificationCenter.default.post(name: .disconnected, object: nil)
            }
        case .connecting:
            DispatchQueue.main.async {
                self.connectingBars()
                self.delegate?.connecting()
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
            case .connectShakeNoResponse:
                message = "Did not receive a response from the server to initial handshake request. Please restart both the mobile and server applications."
            case .discoverTimeout:
                message = "Could not find the server on the local network. Please ensure the server application on the other device is running."
            case .discoverResolveServiceNotFound:
                message = "The resolve service could not be found. Please try again."
            case .discoverResolveBusy:
                message = "The resolve service is busy at this time. Please try again, or restart the mobile app if the issue persists."
            case .discoverIncorrectConfiguration:
                message = "The resolve service was incorrectly configured. Please try again."
            case .discoverResolveCanceled:
                message = "The resolve service was canceled. Please try again."
            case .discoverResolveTimeout:
                message = "The resolve service could not discover the server address in time. Please restart both the mobile and server applications."
            case .discoverResolveFailed:
                message = "Unable to resolve the server's address. Please try again, or restart both the mobile and server applications if the issue persists."
            case .discoverResolveUnknown:
                message = "An error occured while resolving the server IP. Please try again."
            default:
                message = "Please try again. If the issue persists, restart both your mobile device and the server device."
            }
            
            DispatchQueue.main.async {
                self.connectionStrength(strength: 0)
                self.delegate?.disconnected(["title": title, "body": message])
                NotificationCenter.default.post(name: .disconnected, object: nil)
            }
        }
    }
    
    /// Updates the connection strength
    /// Updates view according to connection strength
    /// - Note: `ConnectionServiceDelegate` function implementation
    /// - Parameter strength: Strength percentage
    func connectionStrength(strength: Float) {
        guard ConnectionService.shared.state != .connecting else {
            // Wait until connection secured
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
            guard ConnectionService.shared.state != .disconnected else {
                return
            }
            
            self.delegate?.set25Bar(bar25)
            self.delegate?.set50Bar(bar50)
            self.delegate?.set75Bar(bar75)
            self.delegate?.set100Bar(bar100)
        }
    }
    
    // MARK: Public methods
    /// Opens or closes connection, according to current state
    /// Creates `ConnectionService` delegate instance if not already created
    public func toggleConnection() {
        if ConnectionService.shared.delegate == nil {
            ConnectionService.shared.delegate = self
        }
        
        if ConnectionService.shared.state == .connected {
            stop()
        } else {
            start()
        }
    }
    
    /// Sends message to server using the `ConnectionService`
    /// - Parameter message: Network protocol message to send
    public func sendDirection(_ message: AssistiveTechnologyProtocol) {
        ConnectionService.shared.send(message.rawValue)
    }
    
    // MARK: Private methods
    /// Starts search for connection
    /// Calls the `discover()` function of the `ConnectionService` instance with the name of the custom Bonjour type
    /// This automatically discovers the server if it is running & advertising, then opens a UDP connection to the server.
    /// While discovering the server and until a handshake is received from the server, the state is `connecting`.
    /// Once the connection is open, the state will be `connected`.
    private func start() {
        ConnectionService.shared.discover(type: "_assistive-tech._udp")
    }
    
    /// Close the ConnectionSevice connection, shutdown the server
    private func stop() {
        ConnectionService.shared.close()
    }
    
    /// Set each bar to its on state, one at a time every 0.25 seconds  while the connection is in the `connecting` state to show it is looking for the server
    private func connectingBars() {
        if self.loadingTimer != nil {
            self.loadingTimer?.invalidate()
        }

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
}
