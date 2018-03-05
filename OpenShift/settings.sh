export PROJECT_NAMESPACE=${PROJECT_NAMESPACE:-gcpe-news}
export GIT_URI=${GIT_URI:-"https://github.com/bcgov/gcpe-news-api-client.git"}
export GIT_REF=${GIT_REF:-"master"}

# The templates that should not have their GIT references(uri and ref) over-ridden
# Templates NOT in this list will have they GIT references over-ridden
# with the values of GIT_URI and GIT_REF
export skip_git_overrides=${skip_git_overrides:-"dotnet-20-runtime-centos7-build.json dotnet-20-centos7-build.json"}

# The project components
# - They are all contained under the main OpenShift folder.
export components=${components:-"."}

# The builds to be triggered after buildconfigs created (ones that are not auto-triggered)
export builds=${builds:-""}


# The images to be tagged after build
export images=${images:-""}

# The routes for the project
export routes=${routes:-""}


