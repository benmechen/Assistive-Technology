//
//  View.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 28/01/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import UIKit

/// Test View implementation
/// Any view can be used in its place, as long as it conforms to the ViewModelDelegate protocol to receive updates
class View: UIViewController, ViewModelDelegate {
    
    @IBOutlet weak var start: UIButton!
    @IBOutlet weak var up: UIButton!
    @IBOutlet weak var down: UIButton!
    @IBOutlet weak var left: UIButton!
    @IBOutlet weak var right: UIButton!
    @IBOutlet weak var bar25: UIView!
    @IBOutlet weak var bar50: UIView!
    @IBOutlet weak var bar75: UIView!
    @IBOutlet weak var bar100: UIView!
    
    /// Controlling view model, responsible for providing data and business logic
    var viewModel = ViewModel()
    
    /// Automatically disable directional buttons, set the view model delgate to self
    override func viewDidLoad() {
        super.viewDidLoad()
        // Do any additional setup after loading the view.
        disableDirectionalButtons()
        viewModel.delegate = self
    }

    // MARK: Actions
    /// Toggle the connection to the server
    /// - Parameter sender: Sender
    @IBAction func startStopClient(_ sender: Any) {
        viewModel.toggleConnection()
    }
    
    /// Move the player up
    /// - Parameter sender: Sender
    @IBAction func up(_ sender: Any) {
        viewModel.sendDirection(.up)
    }
    
    /// Move the player down
    /// - Parameter sender: Sender
    @IBAction func down(_ sender: Any) {
        viewModel.sendDirection(.down)
    }
    
    /// Move the player left
    /// - Parameter sender: Sender
    @IBAction func left(_ sender: Any) {
        viewModel.sendDirection(.left)
    }
    
    /// Move the player right
    /// - Parameter sender: Sender
    @IBAction func right(_ sender: Any) {
        viewModel.sendDirection(.right)
    }
    
    // MARK: Protocol functions
    /// Connection opened, set UI
    /// Change button to *"Stop Client"*
    /// Enable directional buttons
    func connected() {
        self.start.setTitle("Stop Client", for: .normal)
        self.enableDirectionalButtons()
    }
    
    /// Connection closed, set UI
    /// Change button to *"Start Client"*
    /// Disable directional buttons
    /// Display reason connection was closed, if any
    /// - Parameter message: Optional dictionary containing values for the error alert title and message; `["title"]` and `["body"]`
    func disconnected(_ message: [String: String]? = nil) {
        self.start.setTitle("Start Client", for: .normal)
        self.disableDirectionalButtons()
        
        if let title = message?["title"], let body = message?["body"] {
            let alert = UIAlertController(title: title, message: body, preferredStyle: .alert)
            alert.addAction(UIAlertAction(title: "OK", style: .default, handler: nil))
            self.present(alert, animated: true, completion: nil)
        }
    }
    
    /// Set the state of the 25% bar
    /// - Parameter on: On or off
    func set25Bar(_ on: Bool) {
        if on {
            self.bar25.backgroundColor = .systemPink
        } else {
            self.bar25.backgroundColor = .lightGray
        }
    }
    
    /// Set the state of the 50% bar
    /// - Parameter on: On or off
    func set50Bar(_ on: Bool) {
        if on {
            self.bar50.backgroundColor = .systemPink
        } else {
            self.bar50.backgroundColor = .lightGray
        }
    }
    
    /// Set the state of the 75% bar
    /// - Parameter on: On or off
    func set75Bar(_ on: Bool) {
        if on {
            self.bar75.backgroundColor = .systemPink
        } else {
            self.bar75.backgroundColor = .lightGray
        }
    }
    
    /// Set the state of the 100% bar
    /// - Parameter on: On or off
    func set100Bar(_ on: Bool) {
        if on {
            self.bar100.backgroundColor = .systemPink
        } else {
            self.bar100.backgroundColor = .lightGray
        }
    }
    
    
    // MARK: Private methods
    /// Enable all directional buttons, set their appearance to inform user they are now available
    private func enableDirectionalButtons() {
        up.tintColor = UIColor.systemPink
        up.isEnabled = true
        down.tintColor = UIColor.systemPink
        down.isEnabled = true
        left.tintColor = UIColor.systemPink
        left.isEnabled = true
        right.tintColor = UIColor.systemPink
        right.isEnabled = true
    }
    
    /// Disable all directional buttons, set their appearance to inform user they are no longer available
    private func disableDirectionalButtons() {
        up.tintColor = UIColor.systemPink.withAlphaComponent(0.5)
        up.isEnabled = false
        down.tintColor = UIColor.systemPink.withAlphaComponent(0.5)
        down.isEnabled = false
        left.tintColor = UIColor.systemPink.withAlphaComponent(0.5)
        left.isEnabled = false
        right.tintColor = UIColor.systemPink.withAlphaComponent(0.5)
        right.isEnabled = false
    }
}

