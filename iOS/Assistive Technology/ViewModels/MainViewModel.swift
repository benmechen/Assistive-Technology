//
//  MainViewModel.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation

/// Interface for the MainViewController to use to communicate with other services
class MainViewModel {
    /// Get the current connection status for the connection icon
    public func getConnectionStatus() -> ConnectionService.State {
        return ConnectionService.shared.state
    }
}
