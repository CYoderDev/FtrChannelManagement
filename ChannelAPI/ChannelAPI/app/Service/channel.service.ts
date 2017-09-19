import { Injectable } from '@angular/core';
import { Http, Response, Headers, RequestOptions, ResponseContentType } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/from';
import { IChannel } from '../Models/channel'

@Injectable()
export class ChannelService {
    constructor(private _http: Http) { }

    get(url: string): Observable<any> {
        return this._http.get(url)
            .map((response: Response) => <any>response.json())
            .catch(this.handleError);
    }

    getBy(url: string, id: string): Observable<any> {
        return this._http.get(url + id)
            .map((response: Response) => <any>response.json())
            .catch(this.handleError);

    }

    getBriefBy(url: string, id: string): Observable<any> {
        return Observable.from(this._http.get(url + id)
            .map((response: Response) => <any>response.json())
            .map((x: IChannel[]) => {
                return new Set(x.map(y => {
                    return { id: y.strFIOSServiceId, num: y.intChannelPosition, name: y.strStationName, call: y.strStationCallSign, logoid: y.intBitMapId };
                }))
            })).catch(this.handleError);
    }

    private handleError(error: Response) {
        console.error(error);
        return Observable.throw(error.json().error || 'Server error');
    }
}