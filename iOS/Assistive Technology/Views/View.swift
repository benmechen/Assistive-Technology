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
    @IBOutlet weak var bar25: UIView!
    @IBOutlet weak var bar50: UIView!
    @IBOutlet weak var bar75: UIView!
    @IBOutlet weak var bar100: UIView!
    
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
        self.start.setTitle("Stop Client", for: .normal)
        self.enableDirectionalButtons()
    }
    
    func disconnected(_ message: [String: String]? = nil) {
        self.start.setTitle("Start Client", for: .normal)
        self.disableDirectionalButtons()
        
        if let title = message?["title"], let body = message?["body"] {
            let alert = UIAlertController(title: title, message: body, preferredStyle: .alert)
            alert.addAction(UIAlertAction(title: "OK", style: .default, handler: nil))
            self.present(alert, animated: true, completion: nil)
        }
    }
    
    func set25Bar(_ on: Bool) {
        if on {
            self.bar25.backgroundColor = .systemPink
        } else {
            self.bar25.backgroundColor = .lightGray
        }
    }
    
    func set50Bar(_ on: Bool) {
        if on {
            self.bar50.backgroundColor = .systemPink
        } else {
            self.bar50.backgroundColor = .lightGray
        }
    }
    
    func set75Bar(_ on: Bool) {
        if on {
            self.bar75.backgroundColor = .systemPink
        } else {
            self.bar75.backgroundColor = .lightGray
        }
    }
    
    func set100Bar(_ on: Bool) {
        if on {
            self.bar100.backgroundColor = .systemPink
        } else {
            self.bar100.backgroundColor = .lightGray
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

