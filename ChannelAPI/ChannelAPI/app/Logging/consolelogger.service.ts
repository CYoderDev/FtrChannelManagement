//declare var console: any;

import { ILogger } from "./default-logger.service";
import { environment } from "../Environments/environment";

export class ConsoleLogService implements ILogger {
    public error(args: any, ...optionalArgs: any[]): void {
        (console && console.error) && console.error(args, ...optionalArgs);
    }      

    public info(args: any, ...optionalArgs: any[]): void {
        if (!environment.production) {
            (console && console.info) && console.info(args, ...optionalArgs);
        }
    }

    public log(args: any, ...optionalArgs: any[]): void {
        if (!environment.production) {
            (console && console.log) && console.log(args, ...optionalArgs);
        }
    }

    public warn(args: any, ...optionalArgs: any[]): void {
        (console && console.warn) && console.warn(args, ...optionalArgs);
    }
}