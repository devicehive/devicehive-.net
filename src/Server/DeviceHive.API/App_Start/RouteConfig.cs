using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace DeviceHive.API
{
    public class RouteConfig
    {
        public static void RegisterRoutes(HttpRouteCollection routes)
        {
            routes.MapHttpRoute(
                name: "UserNetwork",
                routeTemplate: "user/{id}/network/{networkId}",
                defaults: new { controller = "UserNetwork" }
            );

            routes.MapHttpRoute(
                name: "User",
                routeTemplate: "user/{id}",
                defaults: new { controller = "User", id = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "Network",
                routeTemplate: "network/{id}",
                defaults: new { controller = "Network", id = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "DeviceClass",
                routeTemplate: "device/class/{id}",
                defaults: new { controller = "DeviceClass", id = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "Equipment",
                routeTemplate: "device/class/{deviceClassId}/equipment/{id}",
                defaults: new { controller = "Equipment", id = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "DeviceNotificationPoll",
                routeTemplate: "device/{deviceGuid}/notification/poll",
                defaults: new { controller = "DeviceNotificationPoll" }
            );

            routes.MapHttpRoute(
                name: "DeviceNotificationPollMany",
                routeTemplate: "device/notification/poll",
                defaults: new { controller = "DeviceNotificationPoll" }
            );

            routes.MapHttpRoute(
                name: "DeviceNotification",
                routeTemplate: "device/{deviceGuid}/notification/{id}",
                defaults: new { controller = "DeviceNotification", id = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "DeviceCommandIdPoll",
                routeTemplate: "device/{deviceGuid}/command/{id}/poll",
                defaults: new { controller = "DeviceCommandPoll" }
            );

            routes.MapHttpRoute(
                name: "DeviceCommandPoll",
                routeTemplate: "device/{deviceGuid}/command/poll",
                defaults: new { controller = "DeviceCommandPoll" }
            );

            routes.MapHttpRoute(
                name: "DeviceCommand",
                routeTemplate: "device/{deviceGuid}/command/{id}",
                defaults: new { controller = "DeviceCommand", id = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "DeviceEquipment",
                routeTemplate: "device/{id}/equipment/{code}",
                defaults: new { controller = "DeviceEquipment", code = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "Device",
                routeTemplate: "device/{id}",
                defaults: new { controller = "Device", id = RouteParameter.Optional }
            );

            routes.MapHttpRoute(
                name: "Info",
                routeTemplate: "info",
                defaults: new { controller = "ApiInfo" }
            );

            routes.MapHttpRoute(
                name: "Cron",
                routeTemplate: "cron/{action}",
                defaults: new { controller = "Cron" }
            );

            routes.MapHttpRoute(
                name: "Metadata",
                routeTemplate: "metadata",
                defaults: new { controller = "Metadata" }
            );

            routes.MapHttpRoute(
                name: "Home",
                routeTemplate: "",
                defaults: new { controller = "Home" }
            );
        }
    }
}