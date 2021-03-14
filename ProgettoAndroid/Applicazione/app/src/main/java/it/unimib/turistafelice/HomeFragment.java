package it.unimib.turistafelice;


import android.Manifest;
import android.content.Context;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.location.Address;
import android.location.Geocoder;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.SearchView;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.app.ActivityCompat;
import androidx.fragment.app.Fragment;
import androidx.navigation.Navigation;

import com.google.android.gms.maps.CameraUpdateFactory;
import com.google.android.gms.maps.GoogleMap;
import com.google.android.gms.maps.OnMapReadyCallback;
import com.google.android.gms.maps.model.LatLng;
import com.google.android.gms.maps.model.MarkerOptions;
import com.google.android.material.snackbar.Snackbar;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.firestore.FirebaseFirestore;
import com.karumi.dexter.Dexter;
import com.karumi.dexter.PermissionToken;
import com.karumi.dexter.listener.PermissionDeniedResponse;
import com.karumi.dexter.listener.PermissionGrantedResponse;
import com.karumi.dexter.listener.PermissionRequest;
import com.karumi.dexter.listener.single.PermissionListener;
import com.squareup.picasso.Picasso;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.Random;
import java.util.Set;

import it.unimib.turistafelice.databinding.FragmentHomeBinding;
import it.unimib.turistafelice.utils.Constants;


public class HomeFragment extends Fragment implements OnMapReadyCallback {

    private static final String TAG = "HomeFragment";
    private FragmentHomeBinding binding;
    private GoogleMap mGoogleMap;
    private LocationManager locationManager;
    private LocationListener locationListener;
    private LatLng userLatLng;
    private FirebaseAuth fAuth;
    private FirebaseFirestore fStore;
    private String userId;
    private String fullName;


    public HomeFragment() {
        // Required empty public constructor
    }

    public static HomeFragment newInstance() {
        return new HomeFragment();
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        binding = FragmentHomeBinding.inflate(getLayoutInflater());
        binding.mapView.onCreate(savedInstanceState);
        binding.mapView.getMapAsync(this);
        return binding.getRoot();
    }


    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        insertImageIntoView();

        fAuth = FirebaseAuth.getInstance();
        userId = fAuth.getCurrentUser().getUid();
        Context context = getContext();


        SharedPreferences sharedPreferences = context.getSharedPreferences(userId, context.MODE_PRIVATE);

        binding.searchCityView.setOnQueryTextListener(new SearchView.OnQueryTextListener() {
            @Override
            public boolean onQueryTextSubmit(String query) {

                Set<String> interestSet = sharedPreferences.getStringSet(Constants.ALL_USER_INTERESTS, null);
                if(interestSet == null || interestSet.size() == 0){
                    Snackbar.make(view, R.string.search_not_allowed, Snackbar.LENGTH_SHORT).show();
                    return false;
                }
                else {
                    HomeFragmentDirections.SearchPlacesFromCity searchPlacesFromCity =
                            HomeFragmentDirections.searchPlacesFromCity(binding.searchCityView.getQuery().toString());
                    Navigation.findNavController(view).navigate(searchPlacesFromCity);
                    binding.searchCityView.clearFocus();
                    return true;
                }
            }

            @Override
            public boolean onQueryTextChange(String newText) {
                return false;
            }
        });

        binding.buttonGPS.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if(Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
                        && requireActivity().checkSelfPermission(Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED){
                    requestPermissions(new String[] {Manifest.permission.ACCESS_COARSE_LOCATION}, 1000);
                } else{
                    LocationManager locationManager = (LocationManager) requireActivity().getSystemService(Context.LOCATION_SERVICE);
                    Location location = locationManager.getLastKnownLocation(LocationManager.NETWORK_PROVIDER);
                    try{
                        String city = hereLocation(userLatLng.latitude, userLatLng.longitude);
                        Log.d(TAG, "onClick: "+ city);

                        HomeFragmentDirections.SearchPlacesFromCity searchPlacesFromCity =
                                HomeFragmentDirections.searchPlacesFromCity(city);
                        Navigation.findNavController(view).navigate(searchPlacesFromCity);

                    } catch (Exception e){
                        e.printStackTrace();
                    }
                }
            }
        });
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, @NonNull String[] permissions, @NonNull int[] grantResults) {
        switch(requestCode){
            case 1000: {
                if (grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                    LocationManager locationManager = (LocationManager) requireActivity().getSystemService(Context.LOCATION_SERVICE);
                    Location location = locationManager.getLastKnownLocation(LocationManager.NETWORK_PROVIDER);
                    try{
                        String city = hereLocation(userLatLng.latitude, userLatLng.longitude);
                    } catch (Exception e) {
                        e.printStackTrace();
                    }
                } else {
                    Snackbar.make(requireView(), "Denied", Snackbar.LENGTH_SHORT).show();
                }
                break;

            }
        }
    }

    private String hereLocation (double lat, double lon){
        String cityName = "";
        Geocoder geoCoder = new Geocoder(requireActivity(), Locale.getDefault()); //it is Geocoder
        StringBuilder builder = new StringBuilder();
        Log.d(TAG, "onClick: " + userLatLng.latitude + " " + userLatLng.longitude);
        try {
            List<Address> address = geoCoder.getFromLocation(lat, lon, 10);
            Log.d(TAG, "onClick: " + address.get(0).getMaxAddressLineIndex());
            if (address.size() > 0) {
                for (Address adr : address) {
                    if (adr.getLocality() != null
                            && adr.getLocality().length() > 0) {
                        cityName = adr.getLocality();
                        break;
                    }
                }
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
        return cityName;
    }

    private void insertImageIntoView() {
        String path = getPath();
        Picasso.get()
                .load(path)
                .fit()
                .centerCrop()
                .into(binding.imageViewCitySearch);
    }


    private String getPath() {
        List<String> listPath = new ArrayList<>();
        listPath.add("https://i.pinimg.com/564x/d3/5f/2e/d35f2ee3c632bdb0ac3e8266637faffd.jpg");
        listPath.add("https://i.pinimg.com/564x/91/d7/06/91d706f7e5acbc8e5291425028feda55.jpg");
        listPath.add("https://i.pinimg.com/564x/d2/71/28/d271282b289fe88be8ac10d12e4d3011.jpg");

        return listPath.get(new Random().nextInt(listPath.size()));
    }

    @Override
    public void onMapReady(GoogleMap googleMap) {
        mGoogleMap = googleMap;
        mGoogleMap.getUiSettings().setZoomControlsEnabled(true);
        locationManager = (LocationManager) this.requireActivity().getSystemService(Context.LOCATION_SERVICE);
        locationListener = new LocationListener() {
            @Override
            public void onLocationChanged(Location location) {
                userLatLng = new LatLng(location.getLatitude(), location.getLongitude());
                mGoogleMap.clear();
                mGoogleMap.addMarker(new MarkerOptions().position(userLatLng).title("Sei qui"));
                mGoogleMap.moveCamera(CameraUpdateFactory.newLatLngZoom(userLatLng, 10));
            }

            @Override
            public void onStatusChanged(String provider, int status, Bundle extras) {

            }

            @Override
            public void onProviderEnabled(String provider) {

            }

            @Override
            public void onProviderDisabled(String provider) {

            }
        };
        askLocationPermission();

    }

    private void askLocationPermission() {
        Dexter.withContext(getActivity().getBaseContext()).withPermission(Manifest.permission.ACCESS_FINE_LOCATION)
                .withListener(new PermissionListener() {
                    @Override
                    public void onPermissionGranted(PermissionGrantedResponse permissionGrantedResponse) {
                        if (ActivityCompat.checkSelfPermission(getActivity().getBaseContext(),
                                Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED &&
                                ActivityCompat.checkSelfPermission(getActivity().getBaseContext(),
                                        Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {

                            return;
                        }
                        locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 0, 0, locationListener);
                        Location lastLocation =locationManager.getLastKnownLocation(LocationManager.GPS_PROVIDER);
                        if(lastLocation!= null)
                            userLatLng = new LatLng(lastLocation.getLatitude(),lastLocation.getLongitude());
                        else
                            userLatLng = new LatLng(45.517790, 9.212837);
                        mGoogleMap.clear();
                        mGoogleMap.moveCamera(CameraUpdateFactory.newLatLng(userLatLng));

                    }

                    @Override
                    public void onPermissionDenied(PermissionDeniedResponse permissionDeniedResponse) {

                    }

                    @Override
                    public void onPermissionRationaleShouldBeShown(PermissionRequest permissionRequest, PermissionToken permissionToken) {
                        permissionToken.continuePermissionRequest();
                    }
                }).check();
    }

    @Override
    public void onResume() {
        super.onResume();
        binding.mapView.onResume();
    }

    @Override
    public void onPause() {
        super.onPause();
        binding.mapView.onPause();
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        binding.mapView.onDestroy();
    }

    @Override
    public void onSaveInstanceState(Bundle outState) {
        super.onSaveInstanceState(outState);
        binding.mapView.onSaveInstanceState(outState);
    }

    @Override
    public void onLowMemory() {
        super.onLowMemory();
        binding.mapView.onLowMemory();
    }
}
