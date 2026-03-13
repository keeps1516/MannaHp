#!/bin/bash
# Install Stripe CLI for local webhook testing

set -e

# Add Stripe GPG key and apt repo
curl -s https://packages.stripe.dev/api/security/keypair/stripe-cli-gpg/public | gpg --dearmor | sudo tee /usr/share/keyrings/stripe.gpg > /dev/null
echo "deb [signed-by=/usr/share/keyrings/stripe.gpg] https://packages.stripe.dev/stripe-cli-debian-local stable main" | sudo tee /etc/apt/sources.list.d/stripe.list > /dev/null
sudo apt update -qq
sudo apt install -y stripe

echo ""
echo "Stripe CLI installed! Version:"
stripe --version
echo ""
echo "Next step: run 'stripe login' to authenticate"
