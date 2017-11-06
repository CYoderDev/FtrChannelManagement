import { Injectable } from '@angular/core';
import { Http, Response, Headers, RequestOptions, ResponseContentType } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/catch';
import { IChannel } from '../Models/channel'

@Injectable()
export class ChannelService {
    constructor(private _http: Http) { }

    get(url: string): Observable<any> {
        return this._http.get(url)
            .map((response: Response) => <any>response.json())
            .catch(this.handleError);
    }

    getBy(url: string, id: string): Observable<IChannel[]> {
        return this._http.get(url + id)
            .map((reponse: Response) => <IChannel[]>reponse.json())
            .catch(this.handleError);
    }

    getLogo(url: string, id: string): Observable<File> {
        return this._http.get(url + id, { responseType: ResponseContentType.Blob })
            .map((response: Response) => response.blob());
    }

    private handleError(error: Response) {
        console.error(error);
        return Observable.throw(error.json().error || 'Server error');
    }
}