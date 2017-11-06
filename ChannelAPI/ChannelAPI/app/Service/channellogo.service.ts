import { Injectable } from '@angular/core';
import { Http, Response, Headers, RequestOptions, ResponseContentType } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/catch';

@Injectable()
export class ChannelLogoService {
    constructor(private _http: Http) { }

    get(url: string): Observable<any>{
        return this._http.get(url, new RequestOptions({ withCredentials: true }))
            .map((response: Response) => <any>response.json())
            .catch(this.handleError);
    }

    getBy(url: string, id): Observable<any> {
        url = url.replace('{0}', id.toString());
        return this._http.get(url, new RequestOptions({ withCredentials: true }))
            .map((response: Response) => <any>response.json())
            .catch(this.handleError);
    }

    getByBody(url: string, body: File): Observable<any> {
        
        var ret =  this.openLocalImage(body, (val) => {
            return this._http.get(url, new RequestOptions({
                body: val,
                headers: new Headers({ 'Content-Type': 'image/png' }),
                withCredentials: true
            }))
            .map((response: Response) => <any>response.json())
            .catch(this.handleError)
        });

        return ret.readAsDataURL(body);
    }

    putBody(url: string, obj: any): Observable<any> {
        let headers = new Headers({ 'Content-Type': 'image/png' });
        var ret =  this.openLocalImage(obj, (val) => {
                this._http.put(url, val, new RequestOptions({ headers: headers, withCredentials: true }))
                .map((response: Response) => {
                    if (response.ok)
                        return Observable.empty;
                    else
                        throw new Error('Http PUT request failed. Status: ' + response.status + ' - ' + response.statusText);
                })
                .catch(this.handleError);
        });

        return ret.readAsDataURL(obj);
    }

    put(url: string): Observable<any> {
        return this._http.put(url, null, new RequestOptions({ withCredentials: true }))
            .map((response: Response) => {
                if (response.ok)
                    return Observable.empty;
                else
                    throw new Error('Http PUT request failed. Status: ' + response.status + ' - ' + response.statusText);
            })
            .catch(this.handleError);
    }

    post(url: string, obj: any): Observable<any> {
        let headers = new Headers({ 'Content-Type': 'image/png' });

        return this._http.post(url, obj, new RequestOptions({ headers: headers, withCredentials: true }))
            .map((response: Response) => {
                if (response.ok)
                    return Observable.empty;
                else
                    throw new Error('Http POST request failed. Status: ' + response.status + ' - ' + response.statusText);
            });
    }

    performRequest(endPoint: string, method: string, body = null, contentType: string, uploadContentType: string = null): Observable<any> {
        var headers = new Headers({ 'Content-Type': contentType });
        let options = new RequestOptions({ headers: headers, withCredentials: true });
        if (body)
            options.body = body;
        if (uploadContentType)
            headers.append('Upload-Content-Type', uploadContentType);
        if (method == 'GET') {
            headers.append('Cache-Control', 'no-cache,no-store');
            return this._http.get(endPoint, options)
                .map(this.extractData)
                .catch(this.handleError);
        }
        else if (method == 'PUT')
        {
            options.withCredentials = true;
            return this._http.put(endPoint, body, options)
                .map(this.extractData)
                .catch(this.handleError);
        }
        else
        {
            options.withCredentials = true;
            return this._http.post(endPoint, body, options)
                .map(this.extractData)
                .catch(this.handleError);
        }
    }
    
    openLocalImage = function (file, callback): any {
        var fileReader = new FileReader();
        fileReader.onloadend = function (e) {
            return callback(fileReader.result);
        }
        fileReader.readAsDataURL(file);
    }

    convertToBase64(inputValue: any): any {
        var file: File = inputValue;
        var reader = new FileReader();
        var image: any;
        
        reader.onloadend = (e) => {
            e.target["result"];
            return reader.result;
        }

        reader.readAsDataURL(file);
    }

    private extractData(response: Response) {
        var contentType = response.headers.get('Content-Type');
        if (contentType) {
            if (contentType.startsWith('image'))
                return response.text();
        }
        return response.json();
    }

    private handleError(error) {
        console.error(error);
        if (error instanceof Response)
            return Observable.throw(error.statusText || 'Backend Server error');
        else
            return Observable.throw(error || 'Backend Server Error');
    }
}