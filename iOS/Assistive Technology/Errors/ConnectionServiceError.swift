//
//  ConnectionServiceErrors.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 29/01/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation
import Network

/// <#Description#>
enum ConnectionServiceError: Error {
    /// Other connection error
    case connectOther
    /// POSIX address not available
    case connectAddressUnavailable
    /// POSIX permission denied
    case connectPermissionDenied
    /// POSIX device busy
    case connectDeviceBusy
    /// POSIX operation canceled
    case connectCanceled
    /// POSIX connection refused
    case connectRefused
    /// POSIX host is down or unreachable
    case connectHostDown
    /// POSIX connection already exists
    case connectAlreadyConnected
    /// POSIX operation timed ouit
    case connectTimeout
    /// POSIX network is down, unreachable, or has been reset
    case connectNetworkDown
    /// Connection server discovery failed
    case connectShakeNoResponse
    
    /// Discovery search did not find service in time
    case discoverTimeout
    /// Service not found while resolving IP
    case discoverResolveServiceNotFound
    /// Resolve service activity in progress
    case discoverResolveBusy
    /// Resolve service not setup correctly or given bad argument (e.g. bad IP)
    case discoverIncorrectConfiguration
    /// Resolve service was canceled
    case discoverResolveCanceled
    /// Resolve service did not get IP in time
    case discoverResolveTimeout
    /// Unable to resolve IP address from sender
    case discoverResolveFailed
    /// Other, unknown error during IP resolve
    case discoverResolveUnknown
}
