"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
const core_1 = require("@angular/core");
const common_1 = require("@angular/common");
const platform_browser_1 = require("@angular/platform-browser");
const forms_1 = require("@angular/forms");
const ng2_bs3_modal_1 = require("ng2-bs3-modal/ng2-bs3-modal");
const http_1 = require("@angular/http");
const main_1 = require("ag-grid-angular/main");
const app_component_1 = require("./app.component");
const app_routing_1 = require("./app.routing");
const channel_service_1 = require("./Service/channel.service");
const channel_component_1 = require("./Components/channel.component");
const channellogo_component_1 = require("./Components/channellogo.component");
const editlogo_component_1 = require("./Components/editlogo.component");
const channellogo_service_1 = require("./Service/channellogo.service");
const header_component_1 = require("./Components/header.component");
const order_by_pipe_1 = require("./order-by.pipe");
let AppModule = class AppModule {
};
AppModule = __decorate([
    core_1.NgModule({
        imports: [platform_browser_1.BrowserModule, forms_1.ReactiveFormsModule, http_1.HttpModule, app_routing_1.routing, ng2_bs3_modal_1.Ng2Bs3ModalModule, forms_1.FormsModule, main_1.AgGridModule.withComponents([
                header_component_1.HeaderComponent
            ])],
        declarations: [app_component_1.AppComponent, channel_component_1.ChannelComponent, channellogo_component_1.ChannelLogoComponent, order_by_pipe_1.OrderBy, header_component_1.HeaderComponent, editlogo_component_1.EditLogoForm],
        providers: [{ provide: common_1.APP_BASE_HREF, useValue: '/' }, channel_service_1.ChannelService, channellogo_service_1.ChannelLogoService],
        bootstrap: [app_component_1.AppComponent]
    })
], AppModule);
exports.AppModule = AppModule;
