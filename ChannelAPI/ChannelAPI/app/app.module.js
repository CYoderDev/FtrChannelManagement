"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
var core_1 = require("@angular/core");
var common_1 = require("@angular/common");
var platform_browser_1 = require("@angular/platform-browser");
var forms_1 = require("@angular/forms");
var ng2_bs3_modal_1 = require("ng2-bs3-modal");
var http_1 = require("@angular/http");
var main_1 = require("ag-grid-angular/main");
var app_component_1 = require("./app.component");
var app_routing_1 = require("./app.routing");
var channel_service_1 = require("./Service/channel.service");
var channel_component_1 = require("./Components/channel.component");
var editlogo_component_1 = require("./Components/editlogo.component");
var channelinfo_component_1 = require("./Components/channelinfo.component");
var channellogo_service_1 = require("./Service/channellogo.service");
var imagecellrender_component_1 = require("./Components/imagecellrender.component");
var header_component_1 = require("./Components/header.component");
var consolelogger_service_1 = require("./Logging/consolelogger.service");
var default_logger_service_1 = require("./Logging/default-logger.service");
var AppModule = (function () {
    function AppModule() {
    }
    return AppModule;
}());
AppModule = __decorate([
    core_1.NgModule({
        imports: [platform_browser_1.BrowserModule, http_1.HttpModule, app_routing_1.routing, forms_1.FormsModule, ng2_bs3_modal_1.BsModalModule, main_1.AgGridModule.withComponents([
                header_component_1.HeaderComponent,
                imagecellrender_component_1.ImageCellRendererComponent
            ])],
        declarations: [app_component_1.AppComponent, channel_component_1.ChannelComponent, header_component_1.HeaderComponent, editlogo_component_1.EditLogoForm, channelinfo_component_1.ChannelInfoComponent, imagecellrender_component_1.ImageCellRendererComponent, channelinfo_component_1.FocusableInput],
        providers: [{ provide: common_1.APP_BASE_HREF, useValue: '/' }, { provide: default_logger_service_1.Logger, useClass: consolelogger_service_1.ConsoleLogService }, channel_service_1.ChannelService, channellogo_service_1.ChannelLogoService],
        bootstrap: [app_component_1.AppComponent]
    })
], AppModule);
exports.AppModule = AppModule;
