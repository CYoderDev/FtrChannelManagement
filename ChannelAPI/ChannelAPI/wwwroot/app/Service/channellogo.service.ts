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
        return this._http.get(url)
            .map((response: Response) => <any>response.json())
            .catch(this.handleError);
    }

    getBy(url: string, id): Observable<any> {
        url = url.replace('{0}', id.toString());
        return this._http.get(url)
            .map((response: Response) => <any>response.json())
            .catch(this.handleError);
    }

    getByBody(url: string, body): Observable<any> {
        return this._http.get(url, new RequestOptions({
            body: body
        }))
            .map((response: Response) => <any>response.json())
            .catch(this.handleError);
    }

    convertToBase64(inputValue: any): string {
        var file: File = inputValue.files[0];
        var reader = new FileReader();
        var image: string;

        reader.onloadend = (e) => {
            image = reader.result;
        }

        reader.readAsDataURL(file);

        return image;
    }

    private handleError(error: Response) {
        console.error(error);
        return Observable.throw(error.json().error || 'Server error');
    }
}