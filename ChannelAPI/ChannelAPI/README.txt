Author: Cameron Yoder
Company: Frontier Communications
Framework: ASP.NET Core (Backend) / Angular 2 (Frontend)
Date: 11/01/2017

AUTHORIZATION:

- All GET operations are open as there is no sensitive data being returned. 
- All POST/PUT/DELETE operations must meet these specifications:
---The user must provide their credentials for the VHE active directory domain
---The user must be in a security group in the VHE active directory domain called FUI-IMG

INSTALLATION:
- ASP.NET Core Windows Hosting must be installed for the reverse-proxy between IIS and the Kestrel server.
- The IIS Application Pool must be using an identity that has read/write access to the backend IMG database server

***************************
API DOCUMENTATION
***************************

Station API
-	GET: api/station
	o	Returns all FiOS stations
-	GET: api/station/5
	o	Returns station with unique FiOS ID = 5
-	PUT: api/station/5/logo/1211 : image/png or application/octet-stream from body
	o	Updates logo on station with FiOS ID 5 to new bitmap ID, resizes image and saves to channel logo repository as bitmap id
-	PUT: api/station/5/logo/2202 : image/png or application/octet-stream from body
	o	Updates the channel logo and assigns it to the station

Channel Logo API
-	GET: api/channellogo 
	o	Returns all bitmap id’s that are currently in the channel logo repository
-	GET: api/channellogo/2202
	o	Returns image/png file of logo with a bitmap id = 2202
-	GET: api/channellogo/2202/station
	o	Returns all active stations that currently use the logo with bitmap id = 2202
-	GET: api/channellogo/nextid
	o	Returns the next available bitmap id that is not currently being used
-	POST: api/channellogo/2202 : image/png or application/octet-stream from body
	o	Inserts new logo to channel logo repository as 2202.png
			**Note: does not assign to a station
-	PUT: api/channellogo/image/duplicate: image/png or application/octet-stream from body
	o	Gets duplicates of the provided image and returns their bitmap id’s
			**This is a PUT in order to permit passing the image via the body of the request
-	PUT: api/channellogo/image/2202 : image/png or application/octet-stream from body
	o	Updates logo 2202.png to Image provided in body and updates date and MD5 Digest fields in tFIOSBitMap and tFIOSBitmapVersion tables in the database or insert if it doesn’t exist.
			**Note: does not assign to a station, but if already assigned, this will prompt the STB to download bitmap id 2202 from the channel logo repository during next day guide cycle.
-	PUT: api/channellogo/2202
	o	Updates date and MD5 Digest fields in tFIOSBitMap and tFIOSBitmapVersion tables in the database or insert if it doesn’t exist.
			**Note: does not assign to a station, but if already assigned, this will prompt the STB to download bitmap id 2202 from the channel logo repository during next day guide cycle
-	PUT: api/channellogo/2202/station/5
	o	Assigns station with FIOS ID = 5 to bitmap id = 2202 by updating tFIiosBitmapStationMap id and date fields or inserting if it doesn’t exist.
-	DELETE: api/channellogo/2202
	o	Deletes the image file 2202.png from the logo repository and assigns each station that has this bitmap id assigned back to the default bitmap id

Channel API
-	GET: api/channel
	o	Gets all of the FiOS Service ID’s actively assigned to a channel map
-	GET: api/channel/1
	o	Gets the channel information for the channel assigned to FiOS Service ID = 1
-	GET: api/channel/region/93636
	o	Gets all channels in the region with the RegionID = 93636
-	GET: api/channel/genre/2
	o	Gets all channels that are currently assigned a Genre ID = 2
-	GET: api/channel/vho/1
	o	Gets all channels currently assigned to VHO1
-	GET: api/channel/station/abc
	o	Gets all channels with a station name that contains ‘abc’
			**Not case-sensitive
-	GET: api/channel/callsign/abchd
	o	Gets all channels with a station call sign that contains ‘abchd’
			**Not case-sensitive

Region API
-	GET: api/region
	o	Gets all FiOS region information
-	GET: api/region/vho
	o	Gets all VHO names for active VHO’s
			**Active VHOs are defined in the application’s configuration file
-	GET: api/region/active
	o	Gets all region names that are active regions
			**Active regions are defined in the application’s configuration file
