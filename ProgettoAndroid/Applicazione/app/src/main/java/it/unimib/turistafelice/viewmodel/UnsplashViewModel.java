package it.unimib.turistafelice.viewmodel;

import android.util.Log;

import androidx.lifecycle.MutableLiveData;
import androidx.lifecycle.ViewModel;

import java.util.List;
import java.util.Map;

import it.unimib.turistafelice.model.Resource;
import it.unimib.turistafelice.repository.UnsplashRepository;

public class UnsplashViewModel extends ViewModel {

    private static final String TAG = "UnsplashViewModel";
    private MutableLiveData<Resource<Map<String,String>>> photoUrls;

    public MutableLiveData<Resource<Map<String,String>>> getUrl(List<String> cityName) {
        photoUrls = new MutableLiveData<>();
        Log.d(TAG, "getPhotoUrl from Unsplash city " + cityName);
        UnsplashRepository.getInstance().getPhotoUrl(photoUrls, cityName);
        return photoUrls;
    }
}

