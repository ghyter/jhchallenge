PROJECT = RedditChallenge
ENVFILE = ./.devcontainer/.env

setup: ./.devcontainer/.env devcerts

# Create the environment file for the devcontainer.  These values are required for the Reddit API.
./.devcontainer/.env:
	echo "REDDIT_CLIENT_ID=your_client_id" > ${ENVFILE}
	echo "REDDIT_CLIENT_SECRET=your_secret" >> ${ENVFILE}
	echo "REDDIT_REDIRECT_URI=https://localhost:7123/reddit/callback" >> ${ENVFILE}
	@echo "Edit the .env file to add your Reddit API credentials."
	@echo "You can find the .env file at ./.devcontainer/.env"

# Generate the self-signed certificate for HTTPS support
devcerts:
	dotnet dev-certs https --trust
