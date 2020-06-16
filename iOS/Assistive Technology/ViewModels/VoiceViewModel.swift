//
//  VoiceViewModel.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation
import Speech

/// Protocol to pass data down to the view model
protocol VoiceViewModelDelegate {
    func sendAlert(title: String, message: String)
    /// Set the current direction
    /// - Parameter direction: `Direction` enum object
    func setDirection(_ direction: Direction?)
    /// Set the coordinates of the central point
    /// - Parameters:
    ///  - x: X co-ordinate of centre point
    ///  - y: Y co-ordinate of centre point
    func setText(_ text: String)
}

/**
   Performs data processing of sound input to text.
   Acts as an interface between the `VoiceViewController` and services.
*/
class VoiceViewModel {
    /// `VoiceViewController`
    var delegate: VoiceViewModelDelegate?
    
    /// Send a directional message to the server
    /// - Parameter message: `AssistiveTechnologyProtocol` conforming message
    public func sendDirection(_ message: AssistiveTechnologyProtocol) {
        ConnectionService.shared.send(message.rawValue)
    }
    /// Get the current connection status
    public func getConnectionStatus() -> ConnectionService.State {
        return ConnectionService.shared.state
    }
    
    /// Process speech data to text, and pass that text to the `checkForDirectionSaid` func to determine direction
    /// - Parameters:
    ///  - result: The result from the Speech Recognition handler
    ///  - error: Any errors occured during recognition
    public func voiceHandler(result: SFSpeechRecognitionResult?, error: Error?) {
        if let result = result {
            
            let bestString = result.bestTranscription.formattedString
            var lastString: String = ""
            for segment in result.bestTranscription.segments {
                let indexTo = bestString.index(bestString.startIndex, offsetBy: segment.substringRange.location)
                lastString = String(bestString[indexTo...])
            }
            self.delegate?.setText(lastString)
            self.checkForDirectionSaid(resultString: lastString)
        } else if let error = error {
            self.delegate?.sendAlert(title: "Speech Recognizer Error", message: "There has been a speech recognition error.")
            print(error)
        }
    }

    /// Convert text to a direction and tell server & delegate
    /// - Parameter resultString: Input text
    func checkForDirectionSaid(resultString: String) {
         guard let direction = Direction(rawValue: resultString) else {
            self.delegate?.setDirection(nil)
            return
        }
        
        switch direction {
        case .up:
            sendDirection(.up)
        case .right:
            sendDirection(.right)
        case .down:
            sendDirection(.down)
        case .left:
            sendDirection(.left)
        }
        self.delegate?.setDirection(direction)
    }
}
