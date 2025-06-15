#!/usr/bin/env python3
"""
Test script for TimeTracker API screenshot upload endpoint
"""

import requests
import json
import os
import sys

def test_screenshot_upload():
    """Test the screenshot upload endpoint"""
    
    print("🧪 Testing Screenshot Upload endpoint...")
    
    # API endpoint
    url = "http://localhost:5274/api/trackingdata/screenshots"
    
    # Test image path
    image_path = "test-image.png"
    
    # Check if test image exists
    if not os.path.exists(image_path):
        print(f"❌ Test image not found: {image_path}")
        print("Please make sure the image file exists in the current directory")
        return False
    
    print(f"📁 Using test image: {image_path}")
    
    try:
        # Prepare the multipart form data
        with open(image_path, 'rb') as image_file:
            files = {
                'file': ('test-screenshot.png', image_file, 'image/png')
            }
            
            data = {
                'userId': 'test@company.com',
                'sessionId': '1'
            }
            
            print("📤 Uploading screenshot...")
            
            # Make the POST request
            response = requests.post(url, files=files, data=data, timeout=30)
            
            # Check response
            if response.status_code == 200:
                print("✅ Screenshot upload successful!")
                print(f"📊 Response: {response.json()}")
                return True
            else:
                print(f"❌ Screenshot upload failed with status code: {response.status_code}")
                print(f"📄 Response: {response.text}")
                return False
                
    except requests.exceptions.ConnectionError:
        print("❌ Connection failed - make sure the API server is running on http://localhost:5274")
        return False
    except requests.exceptions.Timeout:
        print("❌ Request timed out")
        return False
    except Exception as e:
        print(f"❌ Unexpected error: {str(e)}")
        return False

def test_activity_logs():
    """Test the activity logs endpoint"""
    
    print("\n🧪 Testing Activity Logs endpoint...")
    
    # API endpoint
    url = "http://localhost:5274/api/trackingdata/activity"
    
    # Test data
    activity_data = [
        {
            "timestamp": "2025-06-15T14:30:00Z",
            "eventType": "KEY_DOWN",
            "details": "VK_CODE: 65",
            "userId": "test@company.com",
            "sessionId": "1"
        },
        {
            "timestamp": "2025-06-15T14:30:01Z",
            "eventType": "MOUSE_LEFT_DOWN",
            "details": "X: 100, Y: 200",
            "userId": "test@company.com",
            "sessionId": "1"
        }
    ]
    
    try:
        print("📤 Sending activity logs...")
        
        # Make the POST request
        response = requests.post(
            url, 
            json=activity_data,
            headers={'Content-Type': 'application/json'},
            timeout=30
        )
        
        # Check response
        if response.status_code == 200:
            print("✅ Activity logs upload successful!")
            print(f"📊 Response: {response.json()}")
            return True
        else:
            print(f"❌ Activity logs upload failed with status code: {response.status_code}")
            print(f"📄 Response: {response.text}")
            return False
            
    except requests.exceptions.ConnectionError:
        print("❌ Connection failed - make sure the API server is running on http://localhost:5274")
        return False
    except Exception as e:
        print(f"❌ Unexpected error: {str(e)}")
        return False

def main():
    """Main test function"""
    print("🚀 TimeTracker API Testing Script")
    print("=" * 50)
    
    # Test activity logs endpoint
    activity_success = test_activity_logs()
    
    # Test screenshot upload endpoint
    screenshot_success = test_screenshot_upload()
    
    # Summary
    print("\n" + "=" * 50)
    print("📋 Test Summary:")
    print(f"   Activity Logs: {'✅ PASS' if activity_success else '❌ FAIL'}")
    print(f"   Screenshot Upload: {'✅ PASS' if screenshot_success else '❌ FAIL'}")
    
    if activity_success and screenshot_success:
        print("\n🎉 All tests passed! The API is working correctly.")
        return 0
    else:
        print("\n⚠️  Some tests failed. Please check the API server and try again.")
        return 1

if __name__ == "__main__":
    sys.exit(main())
