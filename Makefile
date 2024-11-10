PROJECT = ReditChallenge
CLIENT = ${PROJECT}/Client
SERVER = ${PROJECT}/Server
SHARED = ${PROJECT}/Shared
TEST_PROJECT = ${PROJECT}/Shared.Tests
SOLUTION_PATH = ./${PROJECT}/${PROJECT}.sln

setup: ./${PROJECT} ./${TEST_PROJECT}
setuptest: ./${TEST_PROJECT}

# Step 1: Create the BlazorWasm project with hosting if not already created
./${PROJECT}:
	dotnet new blazorwasm -o ${PROJECT} --hosted
	# The BlazorWasm command creates ${PROJECT}.sln in the ${PROJECT} directory

# Step 2: Create and add the MSTest project to the existing solution in ${PROJECT} folder
./${TEST_PROJECT}: ./${PROJECT}
	dotnet new mstest -o ${TEST_PROJECT}
	dotnet sln ${SOLUTION_PATH} add ${TEST_PROJECT}/${PROJECT}.Shared.Tests.csproj
	# Add references to both the Server and Shared projects
	dotnet add ${TEST_PROJECT}/${PROJECT}.Shared.Tests.csproj reference ${SERVER}/${PROJECT}.Server.csproj
	dotnet add ${TEST_PROJECT}/${PROJECT}.Shared.Tests.csproj reference ${SHARED}/${PROJECT}.Shared.csproj

clean:
	rm -rf ./${PROJECT}
