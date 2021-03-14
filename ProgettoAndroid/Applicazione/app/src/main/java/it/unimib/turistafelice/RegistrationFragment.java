package it.unimib.turistafelice;

import android.content.Context;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Bundle;
import android.text.TextUtils;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AlertDialog;
import androidx.fragment.app.Fragment;
import androidx.navigation.Navigation;

import com.google.android.gms.tasks.OnCompleteListener;
import com.google.android.gms.tasks.OnFailureListener;
import com.google.android.gms.tasks.OnSuccessListener;
import com.google.android.gms.tasks.Task;
import com.google.android.material.snackbar.Snackbar;
import com.google.firebase.auth.AuthResult;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseUser;
import com.google.firebase.firestore.DocumentReference;
import com.google.firebase.firestore.FirebaseFirestore;
import com.google.firebase.storage.FirebaseStorage;
import com.google.firebase.storage.StorageMetadata;
import com.google.firebase.storage.StorageReference;
import com.google.firebase.storage.UploadTask;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import java.util.regex.Pattern;

import it.unimib.turistafelice.databinding.FragmentRegistrationBinding;
import it.unimib.turistafelice.utils.Constants;

public class RegistrationFragment extends Fragment {

    private static final String TAG = "RegistrationFragment";
    private FragmentRegistrationBinding binding;
    private FirebaseAuth fAuth;
    private FirebaseFirestore fStore;
    private StorageReference storageReference;
    String userId;

    public RegistrationFragment() {
        // Required empty public constructor
    }

    public static RegistrationFragment newInstance(String param1, String param2) {
        RegistrationFragment fragment = new RegistrationFragment();
        return fragment;
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        binding = binding.inflate(getLayoutInflater());
        return binding.getRoot();
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        fAuth = FirebaseAuth.getInstance();
        fStore = FirebaseFirestore.getInstance();


        binding.loginBtn.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                RegistrationFragmentDirections.AlreadyRegisteredAction alreadyRegisteredAction =
                        RegistrationFragmentDirections.alreadyRegisteredAction();
                Navigation.findNavController(view).navigate(alreadyRegisteredAction);
            }
        });

        binding.registerBtn.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                final String email = binding.emailEditText.getText().toString().trim();
                String password = binding.passwordEditText.getText().toString().trim();
                final String fullName = binding.fullNameEditText.getText().toString();
                final String phoneNumber = binding.phoneEditText.getText().toString();


                //controllo i parametri minimi che devono avere i testi che inserisce l utente
                if (TextUtils.isEmpty(email)) {
                    binding.emailEditText.setError(getResources().getString(R.string.email_required));
                    return;
                }

                if (TextUtils.isEmpty(password)) {
                    binding.passwordEditText.setError(getResources().getString(R.string.psw_required));
                    return;
                }

                if (TextUtils.isEmpty(phoneNumber)) {
                    binding.passwordEditText.setError(getResources().getString(R.string.phone_required));
                    return;
                }

                if (password.length() < 6) {
                    binding.passwordEditText.setError(getResources().getString(R.string.invalid_psw));
                    return;
                }


                if (phoneNumber.length() < 2 || !validatePhoneNumber(phoneNumber)) {
                    binding.phoneEditText.setError(getResources().getString(R.string.invalid_phone));
                    return;
                }

                binding.progressBar.setVisibility(View.VISIBLE);

                //Register on db

                fAuth.createUserWithEmailAndPassword(email, password).addOnCompleteListener(new OnCompleteListener<AuthResult>() {
                    @Override
                    public void onComplete(@NonNull Task<AuthResult> task) {
                        if (task.isSuccessful()) {
                            FirebaseUser user = fAuth.getCurrentUser();

                            user.sendEmailVerification().addOnSuccessListener(new OnSuccessListener<Void>() {
                                @Override
                                public void onSuccess(Void aVoid) {


                                    AlertDialog dialog = new AlertDialog.Builder(v.getContext()).setTitle(getResources()
                                            .getString(R.string.verify_email))
                                            .setMessage(getResources().getString(R.string.check_email_address_bos))
                                            .setPositiveButton(getResources().getString(R.string.verified), null)
                                            .setNegativeButton(getResources().getString(R.string.resend_email), null)
                                            .setNeutralButton(getResources().getString(R.string.change_email), null)
                                            .setCancelable(false).show();


                                    Button positiveBtn = dialog.getButton(AlertDialog.BUTTON_POSITIVE);
                                    positiveBtn.setOnClickListener(new View.OnClickListener() {
                                        @Override
                                        public void onClick(View v) {

                                            user.reload().addOnCompleteListener(new OnCompleteListener<Void>() {
                                                @Override
                                                public void onComplete(@NonNull Task<Void> task) {

                                                    FirebaseUser userTask = fAuth.getCurrentUser();

                                                    if (userTask.isEmailVerified()) {

                                                        dialog.dismiss();
                                                        userId = user.getUid();

                                                        Context context = getActivity();
                                                        SharedPreferences sharedPreferences = context.getSharedPreferences(userId, getContext().MODE_PRIVATE);
                                                        SharedPreferences.Editor editor = sharedPreferences.edit();

                                                        Set<String> allUserInterestsSet = new HashSet<>();
                                                        editor.putStringSet(Constants.ALL_USER_INTERESTS, allUserInterestsSet);

                                                        Set<String> allUserTripsSet = new HashSet<>();
                                                        editor.putStringSet(Constants.ALL_TRIPS, allUserTripsSet);
                                                        editor.putString(Constants.FULL_NAME, fullName);

                                                        editor.apply();

                                                        Map<String, Object> userInterestsDoc = new HashMap<>();
                                                        ArrayList<String> allUserInterestsList = new ArrayList<>();
                                                        allUserInterestsList.addAll(allUserInterestsSet);
                                                        userInterestsDoc.put(Constants.ALL_USER_INTERESTS, allUserInterestsList);

                                                        Map<String, Object> userTripsDoc = new HashMap<>();
                                                        ArrayList<String> allUserTripsList = new ArrayList<>();
                                                        allUserTripsList.addAll(allUserTripsList);
                                                        userTripsDoc.put(Constants.ALL_TRIPS, allUserTripsList);


                                                        //inseriamo nella collezione di documenti chiamata "users" nel db un documento contenente l userId
                                                        DocumentReference documentReference = fStore.collection(Constants.USER)
                                                                .document(userId);

                                                        DocumentReference documentReferenceInterests = fStore.collection(Constants.USER)
                                                                .document(userId).collection(Constants.ALL_USER_INTERESTS).document(Constants.ALL_USER_INTERESTS);
                                                        documentReferenceInterests.set(userInterestsDoc);

                                                        DocumentReference documentReferenceTrips = fStore.collection(Constants.USER)
                                                                .document(userId).collection(Constants.ALL_TRIPS).document(Constants.ALL_TRIPS);
                                                        documentReferenceTrips.set(userTripsDoc);

                                                        //creiamo un documento contenente tutti i dati (fullName, phone) riferiti al nostro userId
                                                        //utilizziamo il metodo <chiave, valore>
                                                        Map<String, Object> newUser = new HashMap<>();
                                                        newUser.put(Constants.FNAME, fullName);
                                                        newUser.put(Constants.EMAIL, email);
                                                        newUser.put(Constants.PHONE, phoneNumber);
                                                        newUser.put(Constants.FAVCITY, null);

                                                        //carichiamo i dati nel db
                                                        //ricordiamo di modificare le rules del db, perche di default non ci permette di scriverci sopra
                                                        documentReference.set(newUser).addOnSuccessListener(new OnSuccessListener<Void>() {
                                                            @Override
                                                            public void onSuccess(Void aVoid) {
                                                                Log.d(TAG, "uploaded" + "Account is created for : " + userId);

                                                            }
                                                        });
                                                        storageReference = FirebaseStorage.getInstance().getReference();
                                                        StorageReference fileRef = storageReference
                                                                .child("users/" + fAuth.getCurrentUser().getUid() + "/profile.jpg");

                                                        Uri path = Uri.parse("android.resource://it.unimib.turistafelice/" + R.drawable.ic_profile_default);
                                                        Log.d(TAG, "path: " + path);
                                                        Log.d(TAG, "fileRef: " + fileRef.toString());
                                                        StorageMetadata metadata = new StorageMetadata.Builder()
                                                                .setContentType("image/jpg")
                                                                .build();
                                                        fileRef.putFile(path, metadata).addOnSuccessListener(new OnSuccessListener<UploadTask.TaskSnapshot>() {
                                                            @Override
                                                            public void onSuccess(UploadTask.TaskSnapshot taskSnapshot) {
                                                                Log.d(TAG, "onSuccess: immagine default caricata");
                                                            }
                                                        }).addOnFailureListener(new OnFailureListener() {
                                                            @Override
                                                            public void onFailure(@NonNull Exception e) {
                                                                Log.d(TAG, "onFailure: immagine non caricata "+ e.getMessage());
                                                            }
                                                        });
                                                        Snackbar.make(v, getResources().getString(R.string.account_created),
                                                                Snackbar.LENGTH_SHORT).show();

                                                        RegistrationFragmentDirections.RegisteredLoggedSuccess registeredLoggedSuccess =
                                                                RegistrationFragmentDirections.registeredLoggedSuccess();
                                                        Navigation.findNavController(view).navigate(registeredLoggedSuccess);

                                                    } else {
                                                        Snackbar.make(v, getResources().getString(R.string.email_not_verified),
                                                                Snackbar.LENGTH_SHORT).show();
                                                    }
                                                }
                                            });
                                        }
                                    });


                                    Button negativeBtn = dialog.getButton(AlertDialog.BUTTON_NEGATIVE);
                                    negativeBtn.setOnClickListener(new View.OnClickListener() {
                                        @Override
                                        public void onClick(View v) {
                                            user.sendEmailVerification().addOnSuccessListener(new OnSuccessListener<Void>() {
                                                @Override
                                                public void onSuccess(Void aVoid) {
                                                    Snackbar.make(v, getResources().getString(R.string.verification_sent),
                                                            Snackbar.LENGTH_SHORT).show();
                                                }
                                            }).addOnFailureListener(new OnFailureListener() {
                                                @Override
                                                public void onFailure(@NonNull Exception e) {
                                                    Log.d(TAG, "On Failure" + e.getMessage());
                                                    Snackbar.make(v, "Error: " + e.getMessage(),
                                                            Snackbar.LENGTH_LONG).show();
                                                }
                                            });
                                        }
                                    });

                                    Button neutralBtn = dialog.getButton(AlertDialog.BUTTON_NEUTRAL);
                                    neutralBtn.setOnClickListener(new View.OnClickListener() {
                                        @Override
                                        public void onClick(View v) {
                                            user.delete().addOnCompleteListener(new OnCompleteListener<Void>() {
                                                @Override
                                                public void onComplete(@NonNull Task<Void> task) {
                                                    dialog.dismiss();
                                                    binding.progressBar.setVisibility(View.GONE);
                                                }
                                            });

                                        }
                                    });
                                }
                            }).addOnFailureListener(new OnFailureListener() {
                                @Override
                                public void onFailure(@NonNull Exception e) {
                                    binding.progressBar.setVisibility(View.GONE);
                                    Snackbar.make(v, "Error : " + e.getMessage(),
                                            Snackbar.LENGTH_SHORT).show();
                                }
                            });
                        }
                    }
                }).addOnFailureListener(new OnFailureListener() {
                    @Override
                    public void onFailure(@NonNull Exception e) {
                        Snackbar.make(v, "Error " + e.getMessage(),
                                Snackbar.LENGTH_SHORT).show();
                    }
                });
            }
        });
    }

    private boolean validatePhoneNumber(String phone) {
        if(!Pattern.matches("[a-zA-Z]+", phone)) {
            return phone.length() > 6 && phone.length() <= 13;
        }
        return false;        }
}
