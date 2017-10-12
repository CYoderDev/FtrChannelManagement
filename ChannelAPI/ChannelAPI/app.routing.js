"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var router_1 = require("@angular/router");
var channel_component_1 = require("./Components/channel.component");
var appRoutes = [
    { path: '', redirectTo: 'home', pathMatch: 'full' },
    { path: 'home', component: channel_component_1.ChannelComponent }
];
exports.routing = router_1.RouterModule.forRoot(appRoutes);
