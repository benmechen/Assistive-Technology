//
//  NetServiceBrowser+Timeout.swift
//  Assistive Technology
//
//  Created by Ben Mechen on 08/02/2020.
//  Copyright © 2020 Team 30. All rights reserved.
//

import Foundation
import Network


extension NetServiceBrowser {
    public func searchForServices(ofType type: String, inDomain domain: String, withTimeout delay: Double) {
        self.searchForServices(ofType: type, inDomain: domain)
        
        DispatchQueue.main.asyncAfter(deadline: DispatchTime.now() + delay, execute: {
            if let delegate = self.delegate as? NetServiceBrowserDelegateExtension, delegate.discovered != true {
                self.stop(false)
            }
        })
    }
    
    public func stop(_ success: Bool = false) {
        self.stop()
        if let delegate = self.delegate as? NetServiceBrowserDelegateExtension {
            delegate.netServiceBrowserDidStopSearch?(self, success: success)
        }
    }
}

@objc protocol NetServiceBrowserDelegateExtension: NetServiceBrowserDelegate {
    @objc optional func netServiceBrowserDidStopSearch(_ browser: NetServiceBrowser, success: Bool)
    var discovered: Bool { get set }
}
