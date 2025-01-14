using UnityEngine;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using System.Threading.Tasks;

public class AWSManager : MonoBehaviour
{
    public static AWSManager Instance { get; private set; }

    // Replace with your values from AWS Cognito Console
    private const string IdentityPoolId = "us-east-2:6e8b2e3b-107d-4a58-8552-e85d02aaca21"; // Your Identity Pool ID
    private const string CognitoIdentityRegion = "us-east-2"; // Your Cognito region

    private CognitoAWSCredentials credentials;
    public CognitoAWSCredentials Credentials => credentials;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAWS();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void InitializeAWS()
    {
        try
        {
            credentials = new CognitoAWSCredentials(
                IdentityPoolId,
                RegionEndpoint.GetBySystemName(CognitoIdentityRegion)
            );

            // Force first credentials refresh
            await credentials.GetIdentityIdAsync();
            Debug.Log("AWS Credentials initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing AWS credentials: {e.Message}");
        }
    }
}
