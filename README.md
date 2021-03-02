# FileGrip

High level goal, enable people to use cloud storage providers while making sure their data is not being used by these providers.

2018 article giving a hint about what providers might do with people's data: https://tresorit.com/blog/google-drive-onedrive-dropbox-privacy-policy/

2018 head scratching article about dropbox providing access to aggregated and anonymized data to external scientists: https://www.wired.com/story/dropbox-sharing-data-study-ethics/


The main idea is to add an extra layer of encryption before pushing files to the cloud so that it is not parsable by the provider. The user leverages FileGrip to fetch and decrypt files from cloud providers. He works locally and pushes the changes once done.

## Core principles
- No data is duplicated, copied or diverted in any way by FileGrip.
- The user can easily opt-out of the solution and retrieve the files.
- Data is sacred, guards against data corruption and data loss.

## Features
- Desktop application (local only) (open source component?) (user takes care of secret)
- One click workspace encryption and push to specified cloud provider
- Multi cloud provider supported (Drive, Dropbox, OneDrive)
- Workspace versioning

## Sellable Features
- Cross provider backups
- Saas solution offering web and mobile access, secret vault
- Mobile application accessing the Saas solution to move computing out of the terminal

## WTF Technical Features
- Cross provider sharding for high availability (on top of already highly available services...)

## Questions
- Is it forbidden in the ToS?
- Do the cloud providers have a public API to fetch and push files?

### Technical questions
- How to properly store user credentials (oauth token for example)?
- How to be sure there is no data corruption?
- Do we need to specifically make an effort to ensure data integrity? Do we trust cloud providers? Like they may inject stuff in files.

## POC
- Console app to encrypt and decrypt a file locally using AES
- Console app to push a file to Google Drive
- Console app to fetch a file from Google Drive
- Build using Akka actors

## POC2
- Barebone desktop app to do all actions from previous POC from an interface
- Work on parallelization, should be easy with actors

## POC3
- Reuse akka actors from desktop app to create a web service responsible for fetching/pushing/encryption/decryption
- Build an API to control the webservice
- Work on parallelization, should be easy with actors

## MVP
- Fully tested and functional desktop app

## Promotion
...
