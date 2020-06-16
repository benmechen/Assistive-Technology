//
//  FaceViewModel.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation
import ARKit
import SceneKit
import PulseController

/// Protocol to pass data down to the view model
protocol FaceViewModelDelegate {
    /// Set the current direction
    /// - Parameter direction: `Direction` enum object
    func setDirection(_ direction: Direction?)
    /// Set the coordinates of the central point
    /// - Parameters:
    ///  - x: X co-ordinate of centre point
    ///  - y: Y co-ordinate of centre point
    func setCoordinates(x: CGFloat, y: CGFloat)
}

/**
   Performs data processing of camera input.
   Acts as an interface between the `FaceViewController` and services.
*/
class FaceViewModel {
    /// `FaceViewController`
    var delegate: FaceViewModelDelegate?
    /// Value of currently processed X co-ordinate
    var currentX: CGFloat = 0
    /// Value of currently processed Y co-ordinate
    var currentY: CGFloat = 0
    /// Co-ordinates of hit points (up, right, down, left)
    var hitPoints: [CGPoint] = []
    /// X value PID controller
    var xPulseController: Pulse?
    /// Y value PID controller
    var yPulseController: Pulse?
    
    
    /// Setup PID controllers and direction handling & communication
    init() {
        // Configuration
        let configuration = Pulse.Configuration(minimumValueStep: 0.05, Kp: 1.0, Ki: 0.1, Kd: 0.9)

        // Init PID Controllers
        xPulseController = Pulse(configuration: configuration, measureClosure: { [weak self] () -> CGFloat in
            guard let `self` = self else { return 0 }
            return self.currentX;
        }, outputClosure: { [weak self] (output) in
            guard let `self` = self else { return }

            // Update stored reference to the updated value
            self.currentX = output
            // Inform the delegate and send to server
            let direction = self.calculateDirection(x: self.currentX, y: self.currentY, hitPoints: self.hitPoints)
            
            switch direction {
            case .up:
                self.sendDirection(.up)
            case .right:
                self.sendDirection(.right)
            case .down:
                self.sendDirection(.down)
            case.left:
                self.sendDirection(.left)
            }
            
            self.delegate?.setDirection(direction)
            self.delegate?.setCoordinates(x: self.currentX, y: self.currentY)
        })
        
        yPulseController = Pulse(configuration: configuration, measureClosure: { [weak self] () -> CGFloat in
            guard let `self` = self else { return 0 }
            return self.currentY;
        }, outputClosure: { [weak self] (output) in
            guard let `self` = self else { return }

            // Update stored reference to the updated value
            self.currentY = output
            // Inform the delegate and send to server
            let direction = self.calculateDirection(x: self.currentX, y: self.currentY, hitPoints: self.hitPoints)
            
            switch direction {
            case .up:
                self.sendDirection(.up)
            case .right:
                self.sendDirection(.right)
            case .down:
                self.sendDirection(.down)
            case.left:
                self.sendDirection(.left)
            }
            
            self.delegate?.setDirection(direction)
            self.delegate?.setCoordinates(x: self.currentX, y: self.currentY)
        })
    }
    
    /// Send a directional message to the server
    /// - Parameter message: `AssistiveTechnologyProtocol` conforming message
    public func sendDirection(_ message: AssistiveTechnologyProtocol) {
        ConnectionService.shared.send(message.rawValue)
    }
    /// Get the current connection status
    public func getConnectionStatus() -> ConnectionService.State {
        return ConnectionService.shared.state
    }
    
    /// Takes a projected point on the screen and passes it to the PID controller for processing
    /// - Parameters:
    ///   - point: Vector point on screen
    ///   - centre: Co-ordinate of central point
    ///   - hitPoints: Co-ordinates of up, right, down, left points
    public func handleTrackingInput(point: SCNVector3, centre: CGPoint, hitPoints: [CGPoint]) {
        xPulseController?.setPoint = CGFloat(centre.x + (centre.x - CGFloat(point.x)))
        yPulseController?.setPoint = CGFloat(centre.y + (centre.y - CGFloat(point.y)))
        
        self.hitPoints = hitPoints
    }
    
    /// Calculates direction using closest point to the central point
    /// - Parameters:
    ///  - x: X co-ordinate of the centre point, representing where the user is currently looking
    ///  - y: Y co-oridnate of the point on the screen the user is currently looking at
    ///  - hitPoints: Co-ordinates of the up, right, down, left points on screen
    private func calculateDirection(x: CGFloat, y: CGFloat, hitPoints: [CGPoint]) -> Direction {
        let upXDiff = (x - hitPoints[0].x)*(x - hitPoints[0].x)
        let upYDiff = (y - hitPoints[0].y)*(y - hitPoints[0].y)
        let upDist = sqrtf(Float(upXDiff + upYDiff))
        
        let rightXDiff = (x - hitPoints[1].x)*(x - hitPoints[1].x)
        let rightYDiff = (y - hitPoints[1].y)*(y - hitPoints[1].y)
        let rightDist = sqrtf(Float(rightXDiff + rightYDiff))
        
        let downXDiff = (x - hitPoints[2].x)*(x - hitPoints[2].x)
        let downYDiff = (y - hitPoints[2].y)*(y - hitPoints[2].y)
        let downDist = sqrtf(Float(downXDiff + downYDiff))
        
        let leftXDiff = (x - hitPoints[3].x)*(x - hitPoints[3].x)
        let leftYDiff = (y - hitPoints[3].y)*(y - hitPoints[3].y)
        let leftDist = sqrtf(Float(leftXDiff + leftYDiff))
        
        let dists = [upDist, rightDist, downDist, leftDist]
        
        if dists.min() == leftDist {
            return .left
        } else if dists.min() == downDist {
            return .down
        } else if dists.min() == rightDist {
            return .right
        } else {
            return .up
        }
    }
}
