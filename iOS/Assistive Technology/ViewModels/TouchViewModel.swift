
//  TouchViewModel.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation

/// Interface for the TouchViewController to get service data
class TouchViewModel {
    /// Send a directional message to the server
    /// - Parameter message: `AssistiveTechnologyProtocol` conforming message
    public func sendDirection(_ message: AssistiveTechnologyProtocol) {
        ConnectionService.shared.send(message.rawValue)
    }
    /// Get the current connection status
    public func getConnectionStatus() -> ConnectionService.State {
        return ConnectionService.shared.state
    }
}
