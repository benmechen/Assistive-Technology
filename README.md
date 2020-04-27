# Team 30 - Assistive Technology

[Documentation](https://team30.netlify.com/)

### Git Branches
 * `master` – Protected branch for stable & release versions
 * `staging` – Main development working branch - branch from here to make a new feature
 * `feature/$feature_name` - New feature branch, merge back into staging once done
 * `fix/$feature_fixed` – Fix applied to feature in project
 * `experimental/$feature_name` – Used for testing features, not merged back in to staging

When working on a new change, pull from staging first before making any changes and create a new branch.
Once done with the change, create a new merge request and assign to Ben to review (as GitLab doesn't let you add multiple people as reviewers). If the change is all good we can approve it and merge.
