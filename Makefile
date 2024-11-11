PROJECT = RedditChallenge
CLIENT = ${PROJECT}/Client
CLIENT_FILE = ${PROJECT}/Client/${PROJECT}.Client.csproj
SERVER = ${PROJECT}/Server
SERVER_FILE = ${PROJECT}/Server/${PROJECT}.Server.csproj
SHARED = ${PROJECT}/Shared
SHARED_FILE = ${PROJECT}/Shared/${PROJECT}.Shared.csproj
TEST_PROJECT = ${PROJECT}/Shared.Tests
TEST_PROJECT_FILE = ${PROJECT}/Shared.Tests/${PROJECT}.Shared.Tests.csproj
SOLUTION_PATH = ./${PROJECT}.sln

setup: ./${PROJECT} ./${TEST_PROJECT}
setuptest: ./${TEST_PROJECT}

# Step 1: Create the BlazorWasm project with hosting if not already created
./${PROJECT}:
	dotnet new blazorwasm -o ${PROJECT} --hosted
	# The BlazorWasm command creates ${PROJECT}.sln in the ${PROJECT} directory
	mv ./${PROJECT}/${PROJECT}.sln ./${PROJECT}.sln

# Step 2: Create and add the MSTest project to the existing solution in ${PROJECT} folder
./${TEST_PROJECT}: ./${PROJECT}
	dotnet new mstest -o ${TEST_PROJECT}
	dotnet sln ${SOLUTION_PATH} add ${TEST_PROJECT_FILE}
	# Add references to both the Server and Shared projects
	dotnet add ${TEST_PROJECT_FILE} reference ${SERVER_FILE}
	dotnet add ${TEST_PROJECT_FILE} reference ${SERVER_FILE}

clean:
	rm -rf ./${PROJECT}
