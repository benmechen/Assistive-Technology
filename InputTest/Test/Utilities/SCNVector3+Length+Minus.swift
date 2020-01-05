//
//  SCNVector3+Length+Minus.swift
//  Test
//
//  Created by Ben Mechen on 05/01/2020.
//  Copyright © 2020 Ben Mechen. All rights reserved.
//

import Foundation
import SceneKit

extension SCNVector3 {
    func length() -> Float {
        return sqrtf(x * x + y * y + z * z)
    }
}

func - (l: SCNVector3, r: SCNVector3) -> SCNVector3 {
    return SCNVector3Make(l.x - r.x, l.y - r.y, l.z - r.z)
}
