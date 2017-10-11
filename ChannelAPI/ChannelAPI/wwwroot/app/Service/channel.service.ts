import { Injectable } from '@angular/core';
import { Http, Response, Headers, RequestOptions, ResponseContentType } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/from';
import 'rxjs/add/observable/throw';
import 'rxjs/add/observable/empty';
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

    getBriefBy(id: string): Observable<any> {
        return this._http.get('api/channel/' + id)
            .map((response: Response) => <any>response.json())
            .map((x: IChannel[]) => {
                return x.map(y => {
                    return { id: y.strFIOSServiceId, num: y.intChannelPosition, name: y.strStationName, region: y.strFIOSRegionName, call: y.strStationCallSign, logoid: y.intBitMapId };
                }).pop();
            }).catch(this.handleError);
    }

    put(url: string, obj: any): Observable<any> {
        let body = JSON.stringify(obj);
        let headers = new Headers({ 'Content-Type': 'application/json' });
        let options = new RequestOptions({ headers: headers });

        return this._http.put(url, body, options)
            .map((response: Response) => {
                if (response.ok)
                    return Observable.empty();
                else
                    throw new Error('Http request has failed. Status: ' + response.status + ' - ' + response.statusText);
            })
            .catch(this.handleError);
    }

    private handleError(error: Response) {
        console.error(error);
        return Observable.throw(error.json().error || 'Server error');
    }
}