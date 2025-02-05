import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpService } from './http.service';
import { AssetResponse, ValuesResponse } from "../model/asset/assetResponse";
import { Asset } from '../model/asset/asset';
import { AssetRecommendationInfoResponse } from '../model/asset/assetRecommendationInfoResponse';
import { LocalCacheService } from './local-cache.service';
import { FeedResponse } from '../model/advisor/feedResponse';
import { TerminalAssetResponse } from '../model/asset/terminalAssetResponse';
import { AssetStatusResponse } from '../model/asset/assetStatusResponse';
import { AssetRatingsResponse } from '../model/asset/assetRatingsResponse';

@Injectable()
export class AssetService {
  private getAssetsDetailsUrl = this.httpService.apiUrl("v1/assets/details");
  private getAssetDetailsUrl = this.httpService.apiUrl("v1/assets/{id}/details");
  private getAssetsUrl = this.httpService.apiUrl("v1/assets/");
  private getAssetsReportsUrl = this.httpService.apiUrl("v1/assets/reports");
  private getAssetsEventsUrl = this.httpService.apiUrl("v1/assets/events");
  private followAssetUrl = this.httpService.apiUrl("v1/assets/{id}/followers");
  private getAssetValuesUrl = this.httpService.apiUrl("v1/assets/{id}/values");
  private getTerminalAssetsUrl = this.httpService.apiUrl("v1/assets/terminal");
  private getAssetBaseDataUrl = this.httpService.apiUrl("v1/assets/{id}/basedata");
  private getAssetStatusUrl = this.httpService.apiUrl("v1/assets/{id}/status");
  private getTrendingAssetUrl = this.httpService.apiUrl("v1/assets/trending");
  constructor(private httpService : HttpService, private localCache: LocalCacheService) { }

  getAssetDetails(id: string): Observable<AssetResponse> {
    return this.httpService.get(this.getAssetDetailsUrl.replace("{id}", id.toString()));
  }

  getAssetsDetails(): Observable<AssetResponse[]> {
    return this.httpService.get(this.getAssetsDetailsUrl);
  }

  getTerminalAssets(): Observable<TerminalAssetResponse[]> {
    return this.httpService.get(this.getTerminalAssetsUrl);
  }

  getAssetBaseData(assetId: number): Observable<AssetResponse> {
    return this.httpService.get(this.getAssetBaseDataUrl.replace("{id}", assetId.toString()));
  }

  getAssetStatus(assetId: number): Observable<AssetStatusResponse> {
    return this.httpService.get(this.getAssetStatusUrl.replace("{id}", assetId.toString()));
  }

  getAssetsReports(top?: number, lastReportId?: number, assetId?: number): Observable<FeedResponse[]> {
    var url = this.getAssetsReportsUrl + "?";
    if(!!top) url += "top=" + top;
    if (!!lastReportId) url += "&lastReportId=" + lastReportId;
    if (!!assetId) url += "&assetId=" + assetId;
    return this.httpService.get(url);
  }

  getAssetsEvents(top?: number, lastEventId?: number, assetId?: number): Observable<FeedResponse[]> {
    var url = this.getAssetsEventsUrl + "?";
    if(!!top) url += "top=" + top;
    if (!!lastEventId) url += "&lastEventId=" + lastEventId;
    if (!!assetId) url += "&assetId=" + assetId;
    return this.httpService.get(url);
  }

  getAssetValues(id: number, dateTime?: Date): Observable<ValuesResponse[]> {
    var complement = "";
    if (dateTime) {
      complement = "/?dateTime=" + dateTime.toJSON();
    }
    return this.httpService.get(this.getAssetValuesUrl.replace("{id}", id.toString()) + complement);
  }

  getAssets(): Observable<Asset[]> {
    return this.httpService.get(this.getAssetsUrl);
  }

  followAsset(assetId:number):Observable<void>{
    return this.httpService.post(this.followAssetUrl.replace("{id}", assetId.toString()));
  }

  unfollowAsset(assetId:number):Observable<void>{
    return this.httpService.delete(this.followAssetUrl.replace("{id}", assetId.toString()));
  }

  getTrendingAssets(listSize?: number): Observable<AssetResponse[]> {
    var url = this.getTrendingAssetUrl + "?";
    if(listSize) url += "listSize=" + listSize;
    return this.httpService.get(url);
  }
}
