package it.unimib.turistafelice;

import android.content.DialogInterface;
import android.os.Bundle;
import android.text.TextUtils;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;

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

import it.unimib.turistafelice.databinding.FragmentLoginBinding;
import it.unimib.turistafelice.utils.RefreshUtil;

public class LoginFragment extends Fragment {

    private static final String TAG = "LoginFragment";
    private FirebaseAuth fAuth;
    private FragmentLoginBinding binding;

    public static LoginFragment newInstance() {
        return new LoginFragment();
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        binding = binding.inflate(getLayoutInflater());
        return binding.getRoot();
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        fAuth = FirebaseAuth.getInstance();

        binding.loginBtn.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                String email = binding.emailEditText.getText().toString().trim();
                String password = binding.passwordEditText.getText().toString().trim();


                Log.d(TAG, "email: " + email);
                Log.d(TAG, "pw: " + password);

                if (TextUtils.isEmpty(email)) {
                    binding.emailEditText.setError(getResources().getString(R.string.email_required));
                    return;
                }
                if (TextUtils.isEmpty(password)) {
                    binding.passwordEditText.setError(getResources().getString(R.string.psw_required));
                    return;
                }
                if (password.length() < 6) {
                    binding.passwordEditText.setError(getResources().getString(R.string.invalid_psw));
                    return;
                }

                fAuth.signInWithEmailAndPassword(email, password).addOnCompleteListener(new OnCompleteListener<AuthResult>() {
                    @Override
                    public void onComplete(@NonNull Task<AuthResult> task) {
                        if (task.isSuccessful()) {
                            RefreshUtil.refreshSharedPreferences(fAuth, getActivity(), getContext());
                            LoginFragmentDirections.LoggedActionSucces loggedActionSucces = LoginFragmentDirections.loggedActionSucces();
                            Navigation.findNavController(v).navigate(loggedActionSucces);

                        } else {
                            Snackbar.make(v, getResources().getString(R.string.login_failed), Snackbar.LENGTH_SHORT).show();
                        }
                    }
                });
            }
        });

        binding.forgotPswTextView.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                final EditText resetMail = new EditText(v.getContext());
                androidx.appcompat.app.AlertDialog.Builder passwordResetDialog = new AlertDialog.Builder(v.getContext());
                passwordResetDialog.setTitle(getResources().getString(R.string.reset_psw));
                passwordResetDialog.setMessage(getResources().getString(R.string.enter_email));
                passwordResetDialog.setView(resetMail);

                passwordResetDialog.setPositiveButton(getResources().getString(R.string.si), new DialogInterface.OnClickListener() {
                    @Override
                    public void onClick(DialogInterface dialog, int which) {
                        //estraiamo la mail

                        String mail = resetMail.getText().toString();
                        fAuth.sendPasswordResetEmail(mail).addOnSuccessListener(new OnSuccessListener<Void>() {
                            @Override
                            public void onSuccess(Void aVoid) {
                                Snackbar.make(v, getResources().getString(R.string.resented_email), Snackbar.LENGTH_SHORT).show();
                            }
                        }).addOnFailureListener(new OnFailureListener() {
                            @Override
                            public void onFailure(@NonNull Exception e) {
                                Snackbar.make(v, getResources().getString(R.string.not_resented_email) + e.getMessage(), Snackbar.LENGTH_SHORT).show();

                            }
                        });
                    }
                });

                passwordResetDialog.setNegativeButton("No", new DialogInterface.OnClickListener() {
                    @Override
                    public void onClick(DialogInterface dialog, int which) {

                    }
                });

                passwordResetDialog.create().show();
            }
        });

        binding.registerBtn.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                LoginFragmentDirections.RegisterAction registerAction = LoginFragmentDirections.registerAction();
                Navigation.findNavController(v).navigate(registerAction);
            }
        });
    }
}