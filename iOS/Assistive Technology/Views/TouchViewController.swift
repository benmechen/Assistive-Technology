//
//  TouchViewController.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import UIKit

/**
 Controls the touch input view
 */
class TouchViewController: UIViewController {
    /// Connection status symbol in nav bar
    @IBOutlet weak var connectionStatus: UIBarButtonItem!
    /// Up button
    @IBOutlet weak var upButton: UIButton!
    /// Right button
    @IBOutlet weak var rightButton: UIButton!
    /// Down button
    @IBOutlet weak var downButton: UIButton!
    /// Left button
    @IBOutlet weak var leftButton: UIButton!
    
    /// Processing and service interface
    var viewModel = TouchViewModel()
    
    /// Setup views
    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        NotificationCenter.default.addObserver(self, selector: #selector(setToConnected), name: .connected, object: nil)
        NotificationCenter.default.addObserver(self, selector: #selector(setToDisconnected), name: .disconnected, object: nil)
        
        switch viewModel.getConnectionStatus() {
        case .connected:
            setToConnected()
        default:
            setToDisconnected()
        }
    }
    
    // MARK: Actions
    /// Dismiss the current view
    @IBAction func close(_ sender: Any) {
        self.dismiss(animated: true, completion: nil)
    }
    
    /// Send *up* direction to the server
    @IBAction func up(_ sender: Any) {
        viewModel.sendDirection(.up)
    }
    
    /// Send *right* direction to the server
    @IBAction func right(_ sender: Any) {
        viewModel.sendDirection(.right)
    }
    
    /// Send *down* direction to the server
    @IBAction func down(_ sender: Any) {
        viewModel.sendDirection(.down)
    }
    
    /// Send *left* direction to the server
    @IBAction func left(_ sender: Any) {
        viewModel.sendDirection(.left)
    }
    
    // MARK: Private functions
    /// Set the navigation bar wifi symbol to blue to show connected
    @objc private func setToConnected() {
        connectionStatus.image = UIImage(systemName: "wifi")?.withTintColor(UIColor.systemBlue)
    }
    
    /// Set the navigation bar wifi symbol to black with a cross through to show disconnected
    @objc private func setToDisconnected() {
        connectionStatus.image = UIImage(systemName: "wifi.slash")?.withTintColor(UIColor.black)
    }
}
