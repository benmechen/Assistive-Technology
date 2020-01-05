//
//  EyeViewController.swift
//  Test
//
//  Created by Ben Mechen on 19/12/2019.
//  Copyright © 2019 Ben Mechen. All rights reserved.
//

import UIKit
import ARKit
import SceneKit

class EyeViewController: UIViewController {
    
    @IBOutlet weak var sceneView: ARSCNView!
    @IBOutlet weak var direction: UIImageView!
    @IBOutlet weak var left: Triangle!
    @IBOutlet weak var right: Triangle!
    @IBOutlet weak var up: Triangle!
    @IBOutlet weak var down: Triangle!
    @IBOutlet weak var distance: UILabel!
    
    var contentNode: SCNNode?
    // Multiple copies of the axis origin visualization for the transforms this class visualizes
    lazy var rightEyeNode = SCNReferenceNode(named: "coordinateOrigin")
    lazy var leftEyeNode = SCNReferenceNode(named: "coordinateOrigin")
    var xPositions: [CGFloat] = []
    var yPositions: [CGFloat] = []
    var factor: CGFloat = 1
    var lookAtPositions: [Direction: [Float]] = [
        .right: [],
        .left: [],
        .up: [],
        .down: []
    ]
    var xLookAtPositions: [Float] = []
    var yLookAtPositions: [Float] = []
    
    
    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        
        sceneView.delegate = self
        sceneView.automaticallyUpdatesLighting = true
        
        // Show statistics
        sceneView.showsStatistics = true
        
        direction.layer.cornerRadius = direction.frame.height / 2
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
    
    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)
        factor = self.view.frame.height / self.view.frame.width
    }
}

// MARK: ARSCNViewDelegate
extension EyeViewController: ARSCNViewDelegate {
    
    func renderer(_ renderer: SCNSceneRenderer, nodeFor anchor: ARAnchor) -> SCNNode? {
        // This class adds AR content only for face anchors.
        guard anchor is ARFaceAnchor else { return nil }
    
        // Load an asset from the app bundle to provide visual content for the anchor.
        contentNode = SCNReferenceNode(named: "coordinateOrigin")
        
        // Add content for eye tracking in iOS 12.
        self.addEyeTransformNodes()
        
        // Provide the node to ARKit for keeping in sync with the face anchor.
        return contentNode
    }

    func renderer(_ renderer: SCNSceneRenderer, didUpdate node: SCNNode, for anchor: ARAnchor) {
        guard #available(iOS 12.0, *), let faceAnchor = anchor as? ARFaceAnchor else {
            DispatchQueue.main.async {
                self.right.alpha = 0.5
                self.left.alpha = 0.5
                self.up.alpha = 0.5
                self.down.alpha = 0.5
            }
            return
        }
        
        // Add axis to eyes
        rightEyeNode.simdTransform = faceAnchor.rightEyeTransform
        leftEyeNode.simdTransform = faceAnchor.leftEyeTransform
        
        let rightPoint = sceneView.projectPoint(SCNVector3(
            faceAnchor.rightEyeTransform.columns.2.x,
            faceAnchor.rightEyeTransform.columns.2.y,
            faceAnchor.rightEyeTransform.columns.2.z
        ))
        
        let leftPoint = sceneView.projectPoint(SCNVector3(
            faceAnchor.leftEyeTransform.columns.2.x,
            faceAnchor.leftEyeTransform.columns.2.y,
            faceAnchor.leftEyeTransform.columns.2.z
        ))
        
        // Determine eye direction
        var xDirection: Direction
        var yDirection: Direction
        
        // Calculate distance of the eyes to the camera
        let distanceL = leftEyeNode.worldPosition - SCNVector3Zero
        let distanceR = rightEyeNode.worldPosition - SCNVector3Zero
        
        // Average distance from two eyes
        let distance = (distanceL.length() + distanceR.length()) / 2
        
        let x = faceAnchor.lookAtPoint.x * distance
        let y = faceAnchor.lookAtPoint.y * distance
        
        let smoothThresholdNumber: Int = 10
        xLookAtPositions.append(x)
        xLookAtPositions = Array(xLookAtPositions.suffix(smoothThresholdNumber))
        yLookAtPositions.append(y)
        yLookAtPositions = Array(yLookAtPositions.suffix(smoothThresholdNumber))
        
        if xLookAtPositions.average! > 0 {
            // Positive X - looking right
            xDirection = .right
        } else {
            // Negative X - looking left
            xDirection = .left
        }
        
        if yLookAtPositions.average! > 0 {
            // Positive Y - looking up
            yDirection = .up
        } else {
            // Negative Y - looking down
            yDirection = .down
        }
            
        // Add the latest position and keep up to 10 recent position to smooth with
        self.xPositions.append(CGFloat((rightPoint.x + leftPoint.x) / 2))
        self.yPositions.append(CGFloat((rightPoint.y + leftPoint.y) / 2))
        self.xPositions = Array(self.xPositions.suffix(smoothThresholdNumber))
        self.yPositions = Array(self.yPositions.suffix(smoothThresholdNumber))
                
        DispatchQueue.main.async {
            var xDiff: CGFloat = 0
            var yDiff: CGFloat = 0
            var x: CGFloat = self.xPositions.average!
            var y: CGFloat = self.yPositions.average!
                        
            xDiff = self.view.center.x - CGFloat(x)
            yDiff = self.view.center.y - CGFloat(y)
            
            x = self.view.center.x + xDiff
            y = self.view.center.y + yDiff
            
            if x > self.view.frame.width || x < 0 {
                x = self.view.center.x
            }
            
            if y > self.view.frame.height || y < 0 {
                y = self.view.center.y
            }
            
            self.direction.center = CGPoint(x: x, y: y)

            self.distance.text = "Distance: \(Int(round(distance * 100))) cm"
            
            print("Distance:", distance)
            print("X look at:", faceAnchor.lookAtPoint.x)
            print("Y look at:", faceAnchor.lookAtPoint.y)
                    
            
            // Set sensitivity level
            let difference = abs(faceAnchor.lookAtPoint.x * distance) * Float(self.factor) - abs(faceAnchor.lookAtPoint.y * distance)
            guard abs(difference) >= 0.03 else {
                self.right.alpha = 0.5
                self.left.alpha = 0.5
                self.up.alpha = 0.5
                self.down.alpha = 0.5
                return
            }
            
            // Get most significant direction - rudimentary algorithm
            if abs(faceAnchor.lookAtPoint.x * distance) * Float(self.factor) >= abs(faceAnchor.lookAtPoint.y * distance) {
                print("Greater X")
                switch xDirection {
                case .left:
                    self.left.alpha = 1
                    self.right.alpha = 0.5
                    self.up.alpha = 0.5
                    self.down.alpha = 0.5
                case .right:
                    self.right.alpha = 1
                    self.left.alpha = 0.5
                    self.up.alpha = 0.5
                    self.down.alpha = 0.5
                default:
                    break
                }
            } else {
                switch yDirection {
                case .up:
                    self.up.alpha = 1
                    self.left.alpha = 0.5
                    self.right.alpha = 0.5
                    self.down.alpha = 0.5
                case .down:
                    self.down.alpha = 1
                    self.left.alpha = 0.5
                    self.right.alpha = 0.5
                    self.up.alpha = 0.5
                default:
                    break
                }
            }
            
        }
    }
    
    private func addEyeTransformNodes() {
        guard #available(iOS 12.0, *), let anchorNode = contentNode else { return }
        
        // Scale down the coordinate axis visualizations for eyes.
        rightEyeNode.simdPivot = float4x4(diagonal: float4(3, 3, 3, 1))
        leftEyeNode.simdPivot = float4x4(diagonal: float4(3, 3, 3, 1))
        
        anchorNode.addChildNode(rightEyeNode)
        anchorNode.addChildNode(leftEyeNode)
    }
}
