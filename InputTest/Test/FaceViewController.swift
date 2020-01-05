//
//  ARKitViewController.swift
//  Test
//
//  Created by Ben Mechen on 19/12/2019.
//  Copyright © 2019 Ben Mechen. All rights reserved.
//

import UIKit
import ARKit
import SceneKit

class FaceViewController: UIViewController {
    
    @IBOutlet weak var sceneView: ARSCNView!
    @IBOutlet weak var box: UIView!
    
    var contentNode: SCNNode?
    // Smooth out transformations
    var xPositions: [CGFloat] = []
    var yPositions: [CGFloat] = []
    
    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        
        sceneView.delegate = self
        sceneView.automaticallyUpdatesLighting = true
        
        // Show statistics such as fps and timing information
        sceneView.showsStatistics = true
        
        // Setup gyro
        box.layer.cornerRadius = box.frame.height / 2
    }
    
    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        // Create a session configuration
        let configuration = ARFaceTrackingConfiguration()

        // Run the view's session
        sceneView.session.run(configuration)
    }
    
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        // Pause the view's session
        sceneView.session.pause()
    }
}

// MARK: ARSCNViewDelegate
extension FaceViewController: ARSCNViewDelegate {
    
    func renderer(_ renderer: SCNSceneRenderer, nodeFor anchor: ARAnchor) -> SCNNode? {
        // This class adds AR content only for face anchors.
        guard anchor is ARFaceAnchor else { return nil }
    
        // Load an asset from the app bundle to provide visual content for the anchor.
        contentNode = SCNReferenceNode(named: "coordinateOrigin")
        
        // Provide the node to ARKit for keeping in sync with the face anchor.
        return contentNode
    }

    func renderer(_ renderer: SCNSceneRenderer, didUpdate node: SCNNode, for anchor: ARAnchor) {
        guard #available(iOS 12.0, *), let faceAnchor = anchor as? ARFaceAnchor
            else { return }
        
        let point = sceneView.projectPoint(SCNVector3(
            faceAnchor.transform.columns.2.x,
            faceAnchor.transform.columns.2.y,
            faceAnchor.transform.columns.2.z
        ))
        
        // Add the latest position and keep up to 8 recent position to smooth with.
        let smoothThresholdNumber: Int = 10
        self.xPositions.append(CGFloat(point.x))
        self.yPositions.append(CGFloat(point.y))
        self.xPositions = Array(self.xPositions.suffix(smoothThresholdNumber))
        self.yPositions = Array(self.yPositions.suffix(smoothThresholdNumber))
                
        DispatchQueue.main.async {
            var xDiff: CGFloat = 0
            var x: CGFloat = 0
            var yDiff: CGFloat = 0
            var y: CGFloat = 0
            
            xDiff = self.view.center.x - self.xPositions.average!
            yDiff = self.view.center.y - self.yPositions.average!

            x = self.view.center.x + xDiff
            y = self.view.center.y + yDiff
            
            if x > self.view.frame.width || x < 0 {
                x = self.view.center.x
            }
            
            if y > self.view.frame.height || y < 0 {
                y = self.view.center.y
            }
            
            print(x)
            print(y)
            
            self.box.center = CGPoint(x: x, y: y)
        }
    }
}
