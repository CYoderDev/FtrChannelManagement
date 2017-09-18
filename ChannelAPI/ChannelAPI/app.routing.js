"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const router_1 = require("@angular/router");
const channel_component_1 = require("./Components/channel.component");
const appRoutes = [
    { path: '', redirectTo: 'home', pathMatch: 'full' },
    { path: 'home', component: channel_component_1.ChannelComponent }
];
exports.routing = router_1.RouterModule.forRoot(appRoutes);
