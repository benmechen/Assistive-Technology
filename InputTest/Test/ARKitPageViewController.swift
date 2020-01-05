//
//  ARKitPageViewController.swift
//  Test
//
//  Created by Ben Mechen on 02/01/2020.
//  Copyright © 2020 Ben Mechen. All rights reserved.
//

import UIKit

class ARKitPageViewController: UIPageViewController {

    lazy var orderedViewControllers: [UIViewController] = {
        return [self.createView(withIdentifier: "FaceDemo"),
                self.createView(withIdentifier: "EyeDemo")]
    }()
    
    override func viewDidLoad() {
        super.viewDidLoad()

        // Do any additional setup after loading the view.
        dataSource = self
        
        if let firstViewController = orderedViewControllers.first {
            setViewControllers([firstViewController],
                               direction: .forward,
                               animated: true,
                               completion: nil)
        }
    }
    
    private func createView(withIdentifier id: String) -> UIViewController {
        return UIStoryboard(name: "Main", bundle: nil).instantiateViewController(withIdentifier: id)
    }
}

extension ARKitPageViewController: UIPageViewControllerDelegate, UIPageViewControllerDataSource {
    // MARK: Page View Controller
    
    func pageViewController(_ pageViewController: UIPageViewController, viewControllerBefore viewController: UIViewController) -> UIViewController? {
        guard let viewControllerIndex = orderedViewControllers.index(of: viewController) else {
            return nil
        }
        
        let previousIndex = viewControllerIndex - 1
        
        guard previousIndex >= 0 && orderedViewControllers.count > previousIndex else {
             return nil
        }
        
        return orderedViewControllers[previousIndex]
    }
    
    func pageViewController(_ pageViewController: UIPageViewController, viewControllerAfter viewController: UIViewController) -> UIViewController? {
        guard let viewControllerIndex = orderedViewControllers.index(of: viewController) else {
            return nil
        }
        
        let nextIndex = viewControllerIndex + 1
        let orderedViewControllersCount = orderedViewControllers.count
        
        guard orderedViewControllersCount != nextIndex && orderedViewControllersCount > nextIndex else {
             return nil
        }
                
        return orderedViewControllers[nextIndex]
    }
}
