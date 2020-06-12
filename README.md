# Team 30 - Assistive Technology

[iOS Documentation](https://team30.netlify.com/ios/)
[Windows Documentation](https://team30.netlify.com/windows/)

### Git Branches
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
