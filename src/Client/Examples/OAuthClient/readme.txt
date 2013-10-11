This example uses DeviceHive OAuth authorization to list all devices accessible to the user.

Before running this example application, please make sure you have added an OAuth Client to the DeviceHive.
The OAuth Client entity must have the following properties set:
  - domain: should correspond to the domain of the current application
  - redirectUrl: should correspond to the redurect URL of the current page (http://<domain>/Home/Exchange)
  - oauthId: an arbitrary value

Please make sure you update HomeController constants in accordance to the OAuth Client entity and DeviceHive configuration.

The DeviceHive administrative console should also be running to present the user logon page.
