//
//  VoiceViewController.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import UIKit
import Speech

/**
Controls the Voice Control input view
*/
class VoiceViewController: UIViewController {
    /// Connection status symbol in menu bar
    @IBOutlet weak var connectionStatus: UIBarButtonItem!
    /// Label to show current speech
    @IBOutlet weak var textLabel: UILabel!
    /// Up button
    @IBOutlet weak var up: UIImageView!
    /// Right button
    @IBOutlet weak var right: UIImageView!
    /// Down button
    @IBOutlet weak var down: UIImageView!
    /// Left butotn
    @IBOutlet weak var left: UIImageView!

    /// Interface for input processing and services
    var viewModel = VoiceViewModel()
    /// Gives microphone input
    let audioEngine = AVAudioEngine()
    /// Translates raw microphone input to speech
    let speechRecogniser: SFSpeechRecognizer? = SFSpeechRecognizer()
    /// Request to send off to Apple's servers for recognition
    let request = SFSpeechAudioBufferRecognitionRequest()
    /// State of current recognition task
    var recognitionTask: SFSpeechRecognitionTask? = nil
    
    /// Set up views and begin speech listening
    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        viewModel.delegate = self
        
        NotificationCenter.default.addObserver(self, selector: #selector(setToConnected), name: .connected, object: nil)
        NotificationCenter.default.addObserver(self, selector: #selector(setToDisconnected), name: .disconnected, object: nil)
        
        switch viewModel.getConnectionStatus() {
        case .connected:
            setToConnected()
        default:
            setToDisconnected()
        }
        
        self.requestSpeechAuthorization()
        self.recordAndRecogniseSpeech()
    }
    
    /// Stop listening
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        self.stopRecording()
    }
    
    // MARK: Actions
    /// Dismiss view
    @IBAction func close(_ sender: Any) {
        self.dismiss(animated: true, completion: nil)
    }
    
    // MARK: Speech Recognition
    /// Begin listening for speech, if any found pass off to view model for processing
    public func recordAndRecogniseSpeech() {
        let node = audioEngine.inputNode
        let recordingFormat = node.outputFormat(forBus: 0)
        node.installTap(onBus: 0, bufferSize: 1024, format: recordingFormat) { buffer, _ in
            self.request.append(buffer)
        }
        audioEngine.prepare()
        do {
            try audioEngine.start()
        } catch {
            self.sendAlert(title: "Speech Recognizer Error", message: "There has been an audio engine error.")
            return print(error)
        }
        guard let myRecognizer = SFSpeechRecognizer() else {
            self.sendAlert(title: "Speech Recognizer Error", message: "Speech recognition is not supported for your current locale.")
            return
        }
        if !myRecognizer.isAvailable {
            self.sendAlert(title: "Speech Recognizer Error", message: "Speech recognition is not currently available. Check back at a later time.")
            // Recognizer is not available right now
            return
        }

        recognitionTask = speechRecogniser?.recognitionTask(with: request, resultHandler: { result, error in
            self.viewModel.voiceHandler(result: result, error: error)
        })
    }
    
    /// Stop analysing microphone input
    func stopRecording() {
        recognitionTask?.finish()
        recognitionTask = nil
        
        // stop audio
        request.endAudio()
        audioEngine.stop()
        audioEngine.inputNode.removeTap(onBus: 0)
    }
    
    /// Request user authorisation to capture microphone data
    func requestSpeechAuthorization() {
        SFSpeechRecognizer.requestAuthorization { authStatus in
            OperationQueue.main.addOperation {
                switch authStatus {
                case .denied:
                    self.textLabel.text = "User denied access to speech recognition"
                case .restricted:
                    self.textLabel.text = "Speech recognition restricted on this device"
                case .notDetermined:
                    self.textLabel.text = "Speech recognition not yet authorized"
                default:
                    return
                }
            }
        }
    }
    
    // MARK: Private functions
    /// Set connected icon
    @objc private func setToConnected() {
        connectionStatus.image = UIImage(systemName: "wifi")?.withTintColor(UIColor.systemBlue)
    }
    
    /// Set disconnected icon
    @objc private func setToDisconnected() {
        connectionStatus.image = UIImage(systemName: "wifi.slash")?.withTintColor(UIColor.black)
    }
}

extension VoiceViewController: VoiceViewModelDelegate {
    /// Set text label value to last spoken string
    /// - Parameter text: Value to show
    func setText(_ text: String) {
        self.textLabel.text = text
    }
    
    /// Set a particular direction button to blue
    func setDirection(_ direction: Direction?) {
        switch direction {
        case .up:
            self.up.tintColor = UIColor.systemBlue
            self.left.tintColor = UIColor.systemGray2
            self.down.tintColor = UIColor.systemGray2
            self.right.tintColor = UIColor.systemGray2
        case .left:
            self.left.tintColor = UIColor.systemBlue
            self.up.tintColor = UIColor.systemGray2
            self.down.tintColor = UIColor.systemGray2
            self.right.tintColor = UIColor.systemGray2

        case .down:
            self.down.tintColor = UIColor.systemBlue
            self.left.tintColor = UIColor.systemGray2
            self.up.tintColor = UIColor.systemGray2
            self.right.tintColor = UIColor.systemGray2
        case .right:
            self.right.tintColor = UIColor.systemBlue
            self.left.tintColor = UIColor.systemGray2
            self.down.tintColor = UIColor.systemGray2
            self.up.tintColor = UIColor.systemGray2
        default:
            self.up.tintColor = UIColor.systemGray2
            self.left.tintColor = UIColor.systemGray2
            self.down.tintColor = UIColor.systemGray2
            self.right.tintColor = UIColor.systemGray2
        }
    }
    
    /// Alert user about issue with speech recognition
    func sendAlert(title: String, message: String) {
        let alert = UIAlertController(title: title, message: message, preferredStyle: UIAlertController.Style.alert)
        alert.addAction(UIAlertAction(title: "OK", style: UIAlertAction.Style.default, handler: nil))
        self.present(alert, animated: true, completion: nil)
    }
}
