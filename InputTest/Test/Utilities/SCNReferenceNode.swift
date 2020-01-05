//
//  SCNReferenceNode.swift
//  Test
//
//  Created by Ben Mechen on 24/12/2019.
//  Copyright © 2019 Ben Mechen. All rights reserved.
//

import Foundation
import ARKit

extension SCNReferenceNode {
    convenience init(named resourceName: String, loadImmediately: Bool = true) {
        let url = Bundle.main.url(forResource: resourceName, withExtension: "scn", subdirectory: "Models.scnassets")!
        self.init(url: url)!
        if loadImmediately {
            self.load()
        }
    }
}
