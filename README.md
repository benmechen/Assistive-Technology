# Team 30 - Assistive Technology

[iOS Documentation](https://team30.netlify.com/ios/)

[Windows Documentation](https://team30.netlify.com/windows/)


## iOS Apps

### Description

The client iOS application for the Assistive Technology project. The app contains four inputs:
* Touchscreen buttons
* Gyroscope
* Face tracking
* Voice control

The app automatically discovers and connects to the windows server over the local network when you press "Connect" in the connection window.

### Usage

To run the app, first open the workspace in Xcode. You must have the CocoaPods package manager installed on your system.
The app will only build for iOS 13+ due to the images used. A development certificate is required to sign the build.

## Windows App

### Description

Windows server that handles the inputs sent from the client through the usage of UDP. The initial discovery of the server is done by registering a Zeroconf service. The server simulates two inputs:
* W, A, S & D keys
* Up, Down, Left, Right arrow keys

### Usage
To run the app you must have installed Bonjour Service for Windows on your system. 

You can install the server as a regular application on your system if you use the provided installer (setup.exe) inside the installer folder.

Otherwise, to build and run the application you must use Visual Studio 2019 with the 4.7.2 .NET framework (server is targeted to this version of the framework).

To use it just press the start service button. This will register a Zeroconf service and stablish a UDP connection to the client. If 2 minutes go by without communication between the server and client, the server will close the communication.

#### Uninstall
If you installed the application through the provided installer, you can go to the apps in settings and remove it from there.


## Git Branches
 * `master` – Protected branch for stable & release versions
 * `staging` – Main development working branch - branch from here to make a new feature
 * `feature/$feature_name` - New feature branch, merge back into staging once done
 * `fix/$feature_fixed` – Fix applied to feature in project
 * `experimental/$feature_name` – Used for testing features, not merged back in to staging

When working on a new change, pull from staging first before making any changes and create a new branch.
Once done with the change, create a new merge request and assign to Ben to review (as GitLab doesn't let you add multiple people as reviewers). If the change is all good we can approve it and merge.

##### Example
```
 > git checkout staging            // Switch to staging (working) branch
 > git pull origin staging         // Update your local copy with any changes to remote
 > git checkout -b $BRANCH NAME    // Create a new branch with given name and switch to it
 -- Make any changes --
 > git commit -a -m "$MESSAGE"     // Add all changes and commit with a message describing what you changed
 > git push origin -b $BRANCH_NAME // Push your changes to the remote repo on your new branch

You'll then get a response back with a link in it to create a new merge request, open that link and describe the change and assign to Ben.
If you don't get a link just go to projects.cs.nott.ac.uk and make a merge request manually online after pushing
```