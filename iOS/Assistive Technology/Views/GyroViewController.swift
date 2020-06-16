//
//  GyroViewController.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import UIKit
import CoreMotion

/**
 Controls the Gyroscope input view
 */
class GyroViewController: UIViewController {
    /// Connection status symbol in menu bar
    @IBOutlet weak var connectionStatus: UIBarButtonItem!
    /// Slider to control gyro sensitivity
    @IBOutlet weak var sensitivitySlider: UISlider!
    /// Up button
    @IBOutlet weak var up: UIImageView!
    /// Right button
    @IBOutlet weak var right: UIImageView!
    /// Down button
    @IBOutlet weak var down: UIImageView!
    /// Left button
    @IBOutlet weak var left: UIImageView!
    /// Central point
    @IBOutlet weak var center: UIImageView!
    
    /// Interface for input processing and services
    var viewModel = GyroViewModel()
    /// Device motion handler
    var motionManager: CMMotionManager!

    /// Set up views and motion handler
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
        
        sensitivitySlider.minimumValue = 0
        sensitivitySlider.maximumValue = 1
        
        motionManager = CMMotionManager()
        
        if motionManager.isDeviceMotionAvailable {
            motionManager.deviceMotionUpdateInterval = 0.01
            motionManager.startDeviceMotionUpdates(to: .main) {
                [weak self] (data, error) in

                guard let data = data, error == nil else {
                    return
                }
                
                guard (self != nil) else {
                    return
                }
                
                // Pass to view model for processing
                self?.viewModel.handleGyroInput(data: data, centre: self!.view.center, hitPoints: [self!.up.center, self!.right.center, self!.down.center, self!.left.center], sensitivity: self?.sensitivitySlider.value ?? 0.5)
            }
        }
    }
    
    /// Stop motion updates
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        motionManager.stopDeviceMotionUpdates()
    }
    
    // MARK: Actions
    /// Close screen
    @IBAction func close(_ sender: Any) {
        self.dismiss(animated: true, completion: nil)
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

extension GyroViewController: GyroViewModelDelegate {
    /// Set co-ordinates of centre point
    func setCoordinates(x: CGFloat, y: CGFloat) {
        self.center.center = CGPoint(x: x, y: y)
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
            self.center.tintColor = UIColor.systemBlue
        }
    }
}
