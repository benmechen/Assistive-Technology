//
//  View.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 28/01/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import UIKit

class View: UIViewController, ViewModelDelegate {
    
    @IBOutlet weak var start: UIButton!
    @IBOutlet weak var up: UIButton!
    @IBOutlet weak var down: UIButton!
    @IBOutlet weak var left: UIButton!
    @IBOutlet weak var right: UIButton!
    
    var viewModel = ViewModel()
    
    override func viewDidLoad() {
        super.viewDidLoad()
        // Do any additional setup after loading the view.
        disableDirectionalButtons()
        viewModel.delegate = self
    }

    // MARK: Actions
    @IBAction func startStopClient(_ sender: Any) {
        viewModel.toggleConnection()
    }
    
    @IBAction func up(_ sender: Any) {
        viewModel.sendDirection(.up)
    }
    
    @IBAction func down(_ sender: Any) {
        viewModel.sendDirection(.down)
    }
    
    @IBAction func left(_ sender: Any) {
        viewModel.sendDirection(.left)
    }
    
    @IBAction func right(_ sender: Any) {
        viewModel.sendDirection(.right)
    }
    
    // MARK: Protocol functions
    func connected() {
        DispatchQueue.main.async {
            self.start.setTitle("Stop Client", for: .normal)
            self.enableDirectionalButtons()
        }
    }
    
    func disconnected() {
        DispatchQueue.main.async {
            self.start.setTitle("Start Client", for: .normal)
            self.disableDirectionalButtons()
        }
    }
    
    
    // MARK: Private methods
    
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

