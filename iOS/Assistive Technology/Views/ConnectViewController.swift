//
//  ConnectViewController.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import UIKit

class ConnectViewController: UIViewController {
    /// Navigation bar
    @IBOutlet weak var navBar: UINavigationBar!
    /// Title
    @IBOutlet weak var titleLabel: UILabel!
    /// Connection button
    @IBOutlet weak var connect: UIButton!
    /// First connection bar
    @IBOutlet weak var bar25: UIView!
    /// Second connection bar
    @IBOutlet weak var bar50: UIView!
    /// Third connection bar
    @IBOutlet weak var bar75: UIView!
    /// Fourth connection bar
    @IBOutlet weak var bar100: UIView!
    
    /// Interface for services
    var viewModel = ConnectViewModel()
    
    /// View loaded, set up buttons and title
    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        viewModel.delegate = self

        navBar.setBackgroundImage(UIImage(), for: UIBarMetrics.default)
        navBar.shadowImage = UIImage()

        connect.layer.cornerRadius = 10
        
        switch viewModel.getConnectionState() {
        case .connected:
            self.connected()
        case .connecting:
            self.connecting()
        case .disconnected, .failed:
            self.disconnected()
        }
    }
    
    // MARK: Actions
    /// Dismiss the current screen
    @IBAction func close(_ sender: Any) {
        self.dismiss(animated: true, completion: nil)
    }
    
    /// User has pressed connect button, toggle the connection
    @IBAction func startStopClient(_ sender: Any) {
        viewModel.toggleConnection()
    }
}

// MARK: Protocol Functions
extension ConnectViewController: ConnectViewModelDelegate {
    /// Connection state changed to connecting
    /// Update button text
    func connecting() {
        self.titleLabel.text = "Connect"
        self.connect.setTitle("Connecting...", for: .normal)
    }
    
    /// Connection opened, set UI
    /// Change button to *"Disconnect"*
    func connected() {
        self.titleLabel.text = "Connected"
        self.connect.setTitle("Disconnect", for: .normal)
    }
    
    /// Connection closed, set UI
    /// Change button to *"Start Client"*
    /// Disable directional buttons
    /// Display reason connection was closed, if any
    /// - Parameter message: Optional dictionary containing values for the error alert title and message; `["title"]` and `["body"]`
    func disconnected(_ message: [String: String]? = nil) {
        self.titleLabel.text = "Connect"
        self.connect.setTitle("Connect", for: .normal)
        
        if let title = message?["title"], let body = message?["body"] {
            let alert = UIAlertController(title: title, message: body, preferredStyle: .alert)
            alert.addAction(UIAlertAction(title: "OK", style: .default, handler: nil))
            self.present(alert, animated: true, completion: nil)
        }
        
        set25Bar(false)
        set50Bar(false)
        set75Bar(false)
        set100Bar(false)
    }
    
    /// Set the state of the 25% bar
    /// - Parameter on: On or off
    func set25Bar(_ on: Bool) {
        if on {
            self.bar25.backgroundColor = .systemBlue
        } else {
            self.bar25.backgroundColor = .lightGray
        }
    }
    
    /// Set the state of the 50% bar
    /// - Parameter on: On or off
    func set50Bar(_ on: Bool) {
        if on {
            self.bar50.backgroundColor = .systemBlue
        } else {
            self.bar50.backgroundColor = .lightGray
        }
    }
    
    /// Set the state of the 75% bar
    /// - Parameter on: On or off
    func set75Bar(_ on: Bool) {
        if on {
            self.bar75.backgroundColor = .systemBlue
        } else {
            self.bar75.backgroundColor = .lightGray
        }
    }
    
    /// Set the state of the 100% bar
    /// - Parameter on: On or off
    func set100Bar(_ on: Bool) {
        if on {
            self.bar100.backgroundColor = .systemBlue
        } else {
            self.bar100.backgroundColor = .lightGray
        }
    }
}
