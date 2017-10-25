"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var environment_1 = require("../Environments/environment");
var ConsoleLogService = (function () {
    function ConsoleLogService() {
    }
    ConsoleLogService.prototype.error = function (args) {
        var optionalArgs = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            optionalArgs[_i - 1] = arguments[_i];
        }
        (console && console.error) && console.error.apply(console, [args].concat(optionalArgs));
    };
    ConsoleLogService.prototype.info = function (args) {
        var optionalArgs = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            optionalArgs[_i - 1] = arguments[_i];
        }
        if (!environment_1.environment.production) {
            (console && console.info) && console.info.apply(console, [args].concat(optionalArgs));
        }
    };
    ConsoleLogService.prototype.log = function (args) {
        var optionalArgs = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            optionalArgs[_i - 1] = arguments[_i];
        }
        if (!environment_1.environment.production) {
            (console && console.log) && console.log.apply(console, [args].concat(optionalArgs));
        }
    };
    ConsoleLogService.prototype.warn = function (args) {
        var optionalArgs = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            optionalArgs[_i - 1] = arguments[_i];
        }
        (console && console.warn) && console.warn.apply(console, [args].concat(optionalArgs));
    };
    return ConsoleLogService;
}());
exports.ConsoleLogService = ConsoleLogService;
