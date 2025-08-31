# Getting Started with XCross-Feed

This backend was built with ASP.core .net 9.0, it pulls posts from social media profiles, parses them, then broadcasts them to WebSocket clients.

# Quickstart Guide
1. Clone the project from this repository.

`gh repo clone adrnd/xcross-feed`
   
3. Clone the front-end from https://github.com/adrnd/xcross-feed.git

`gh repo clone adrnd/xcross-frontend`
3. Create a free account for the [Twitter AIO API](https://rapidapi.com/viperscores-viperscores-default/api/twitter-aio) and note down the API host domain, API key and the RequestUri for the GET endpoint "Tweets by username". 
   (other services which use the same format might use as well)

## Backend 
### Option 1: Docker
1. Adjust dockerfile (in the rootfolder) and expose the port to a free port that you want to use. (Docker Default is 8080)
2. Create and image of the Dockerfile. In VS Code, you can install the Docker Extension and right-click dockerfile in the folder, give it a tag.  
3. After the image has been created, open Docker and execute the image to create a container. Map the port to **8080**:8080 (or a port of your choice). Add the environment variable with your Twitter AIO API information (see above) and the list of usernames you want to follow (maximum is 3).

The following environment variables are necessary:
`ApiKey_TAIO`
`ApiHost_TAIO`
`ApiRequestUri_TAIO`
`USER1`

These are optional:

`USER2`
`USER3`

<img width="546" height="721" alt="image" src="https://github.com/user-attachments/assets/815fef86-f4f4-4e29-826b-005b34640859" />

`ApiKey_TAIO`
`ApiHost_TAIO`
`ApiRequestUri_TAIO`
`USER1`
`USER2`
`USER3``

4. Hit run, your server should then be available almost immediately.

### Option 2: Visual Studio
To debug and further develop the backend server, I'd recommend using Visual Studio.
1. Open Visual Studio and choose "Open Solution". Pick the file **xcross-backend.sln** in the rootfolder.
2. Rightclick the project "xcross-backend" and click "Manage User Secrets".

<img width="460" height="422" alt="image" src="https://github.com/user-attachments/assets/cede8ed5-ec76-419f-b1a1-fb8e62f1a1df" />

3. Edit the secrets file that should open in the editor and enter the Environment Variables that you've received from the Twitter AIO API site.
4. Enter the variables in the following format, and click save:

`{
"ApiKey_TAIO":"abc",
"ApiHost_TAIO":"abc.com",
"ApiRequestUri_TAIO":"https://abc",
"USER1":"elonmusk", //...
}`

5. Change the launch parameters to "Debug" and choose HTTP or HTTPS, keep in mind that the default ports are 5013 (HTTP) or 7191 (HTTPS).
6. Launch the Debugging process.

   <img width="583" height="132" alt="image" src="https://github.com/user-attachments/assets/020967e4-f568-440c-87a1-df7f9e3e342c" />


## Frontend
1. Make sure you have npm installed.
2. Open the file in xcross-frontend\src\components\TweetList.tsx in any editor, locate the variable 

`const WS_URL = 'ws://localhost:5013/ws'
`

2. Adjust the protocol (ws = HTTP / wss = HTTPS) and the backend port according to the settings you chose in Visual Studio or the Dockerfile.
3. Enter the folder > cd ./xcross-frontend run the app in the terminal by running:

`npm start`

4. Your browser should open automatically, if not, open [http://localhost:3000](http://localhost:3000)

The Xcross Live Feed should now auto-populate and update as long as the backend server is still running. 

