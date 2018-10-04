import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpService } from './http.service';
import { AssetResponse, ValuesResponse } from "../model/asset/assetResponse";
import { Asset } from '../model/asset/asset';
import { AssetRecommendationInfoResponse } from '../model/asset/assetRecommendationInfoResponse';
import { LocalCacheService } from './local-cache.service';
import { ReportResponse } from '../model/asset/reportResponse';

@Injectable()
export class AssetService {
  private getAssetsDetailsUrl = this.httpService.apiUrl("v1/assets/details");
  private getTrendingAssetsUrl = this.httpService.apiUrl("v1/assets/trending");
  private getAssetDetailsUrl = this.httpService.apiUrl("v1/assets/{id}/details");
  private getAssetRecommendationInfoUrl = this.httpService.apiUrl("v1/assets/{id}/recommendation_info");
  private getAssetsUrl = this.httpService.apiUrl("v1/assets/");
  private getAssetsReportsUrl = this.httpService.apiUrl("v1/assets/reports");
  private followAssetUrl = this.httpService.apiUrl("v1/assets/{id}/followers");
  private getAssetValuesUrl = this.httpService.apiUrl("v1/assets/{id}/values");
  constructor(private httpService : HttpService, private localCache: LocalCacheService) { }

  getAssetDetails(id: string): Observable<AssetResponse> {
    return this.httpService.get(this.getAssetDetailsUrl.replace("{id}", id.toString()));
  }

  getAssetsDetails(): Observable<AssetResponse[]> {
    return this.httpService.get(this.getAssetsDetailsUrl);
  }

  getAssetsReports(top?: number, lastReportId?: number): Observable<ReportResponse[]> {
    var url = this.getAssetsReportsUrl + "?";
    if(!!top) url += "top=" + top;
    if (!!lastReportId) url += "&lastReportId=" + lastReportId;
    return this.httpService.get(url);
  }

  getAssetValues(id: number, dateTime?: Date): Observable<ValuesResponse[]> {
    var complement = "";
    if (dateTime) {
      complement = "/?dateTime=" + dateTime.toJSON();
    }
    return this.httpService.get(this.getAssetValuesUrl.replace("{id}", id.toString()) + complement);
  }

  getTrendingAssets(): Observable<AssetResponse[]> {
    return this.localCache.requestWithCache(this.httpService.get(this.getTrendingAssetsUrl), this.getTrendingAssetsUrl);
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
  
  getAssetRecommendationInfo(assetId: number): Observable<AssetRecommendationInfoResponse> {
    return this.httpService.get(this.getAssetRecommendationInfoUrl.replace("{id}", assetId.toString()));
  }
}
