export interface ILogger {
    error(args: any, ...optionalArgs: any[]): void;
    info(args: any, ...optionalArgs: any[]): void;
    log(args: any, ...optionalArgs: any[]): void;
    warn(args: any, ...optionalArgs: any[]): void;
}

export class Logger implements ILogger {
    public error(args: any, ...optionalArgs: any[]): void {
        //default logger does no work
    }

    public info(args: any, ...optionalArgs: any[]): void {
        //default logger does no work
    }

    public log(args: any, ...optionalArgs: any[]): void {
        //default logger does no work
    }

    public warn(args: any, ...optionalArgs: any[]): void {
        //default logger does no work
    }
}