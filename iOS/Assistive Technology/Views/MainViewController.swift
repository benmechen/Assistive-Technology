//
//  MainViewController.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import UIKit

/**
Controls the Main input table view
*/
class MainViewController: UITableViewController {
    /// Connection status symbol in menu bar
    @IBOutlet weak var connectionStatus: UIBarButtonItem!
    
    /// Interface for services
    var viewModel = MainViewModel()
    
    /// Set up views
    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        tableView.tableFooterView = UIView()
        
        NotificationCenter.default.addObserver(self, selector: #selector(setToConnected), name: .connected, object: nil)
        NotificationCenter.default.addObserver(self, selector: #selector(setToDisconnected), name: .disconnected, object: nil)
        
        switch viewModel.getConnectionStatus() {
        case .connected:
            setToConnected()
        default:
            setToDisconnected()
        }

    }
    
    // MARK: Table Settings
    /// Deselect row after user has pressed one
    override func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        tableView.deselectRow(at: indexPath, animated: true)
    }
    
    // MARK: Private functions
    /// Set connected icon
    @objc private func setToConnected() {
        connectionStatus.image = UIImage(systemName: "wifi")?.withTintColor(UIColor.systemBlue)
    }
    
    /// Set the navigation bar wifi symbol to black with a cross through to show disconnected
    @objc private func setToDisconnected() {
        connectionStatus.image = UIImage(systemName: "wifi.slash")?.withTintColor(UIColor.black)
    }
}
