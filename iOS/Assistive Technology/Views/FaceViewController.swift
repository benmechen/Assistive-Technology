//
//  FaceViewController.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 14/06/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import UIKit
import ARKit
import SceneKit

/**
Controls the Face Tracking input view
*/
class FaceViewController: UIViewController {
    /// Connection status symbol in menu bar
    @IBOutlet weak var connectionStatus: UIBarButtonItem!
    /// Face tracking scene to project points onto
    @IBOutlet weak var sceneView: ARSCNView!
    /// Up button
    @IBOutlet weak var up: UIImageView!
    /// Right button
    @IBOutlet weak var right: UIImageView!
    /// Down button
    @IBOutlet weak var down: UIImageView!
    /// Left button
    @IBOutlet weak var left: UIImageView!
    /// Central point
    @IBOutlet weak var centre: UIImageView!
    
    /// Interface for input processing and services
    var viewModel = FaceViewModel()
    
    /// Set up views
    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        NotificationCenter.default.addObserver(self, selector: #selector(setToConnected), name: .connected, object: nil)
        NotificationCenter.default.addObserver(self, selector: #selector(setToDisconnected), name: .disconnected, object: nil)
        
        viewModel.delegate = self
        
        switch viewModel.getConnectionStatus() {
        case .connected:
            setToConnected()
        default:
            setToDisconnected()
        }
        
        sceneView.delegate = self
        sceneView.automaticallyUpdatesLighting = true
        
        // Show statistics such as fps and timing information
        sceneView.showsStatistics = true
    }
    
    /// Configure face tracking and run it
    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        // Create a session configuration
        let configuration = ARFaceTrackingConfiguration()

        // Run the view's session
        sceneView.session.run(configuration)
    }
    
    /// Stop tracking and scene updates
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        // Pause the view's session
        sceneView.session.pause()
    }

    
    // MARK: Actions
    /// Dismiss view
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

extension FaceViewController: ARSCNViewDelegate {
    /// Scene has updated with a new anchor point for the face.
    /// Project the new achor point vector from a real world, 3D position to the screen's 2D plane to get the point on the screen the user is looking at
    /// Pass this 2D point to the view model for processing
    func renderer(_ renderer: SCNSceneRenderer, didUpdate node: SCNNode, for anchor: ARAnchor) {
        guard #available(iOS 12.0, *), let faceAnchor = anchor as? ARFaceAnchor
            else { return }
        
        let point = sceneView.projectPoint(SCNVector3(
            faceAnchor.transform.columns.2.x,
            faceAnchor.transform.columns.2.y,
            faceAnchor.transform.columns.2.z
        ))
        
        
        
        DispatchQueue.main.async {
            self.viewModel.handleTrackingInput(point: point, centre: self.view.center, hitPoints: [self.up.center, self.right.center, self.down.center, self.left.center])
        }
    }
}

extension FaceViewController: FaceViewModelDelegate {
    /// Set a particular direction button to blue
    func setDirection(_ direction: Direction?) {
        DispatchQueue.main.async {
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
                break
            }
        }
    }
    
    /// Set co-ordinates of centre point
    func setCoordinates(x: CGFloat, y: CGFloat) {
        centre.center = CGPoint(x: x, y: y)
    }
}
